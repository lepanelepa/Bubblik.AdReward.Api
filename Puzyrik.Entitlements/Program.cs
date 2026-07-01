// Puzyrik.Entitlements — Program.cs
// REPLACES the template-generated Program.cs in this project.

using Microsoft.Extensions.Caching.Hybrid;
using Puzyrik.Entitlements.Billing;
using Puzyrik.Entitlements.Data;
using Puzyrik.Entitlements.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource(connectionName: "entitlementsdb");

builder.Services.Configure<GooglePlayOptions>(
    builder.Configuration.GetSection(GooglePlayOptions.SectionName));

builder.Services.AddScoped<EntitlementRepository>();

// Swap the verifier by environment: a fake locally (no Play Console needed), the real
// Google Play client in every other environment. The interface is also what makes the
// verify flow unit-testable.
if (builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<IGooglePlayVerifier, FakeGooglePlayVerifier>();
else
    builder.Services.AddSingleton<IGooglePlayVerifier, GooglePlayVerifier>();

// Hot read-path cache for entitlement lookups (HybridCache: in-memory + optional distributed).
builder.Services.AddHybridCache();

builder.Services.AddHealthChecks();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.MapDefaultEndpoints();
app.MapHealthChecks("/healthz");

// Called by the game right after the $1 remove-ads non-consumable purchase completes on-device.
app.MapPost("/purchases/google/verify", async (
    VerifyPurchaseRequest request,
    IGooglePlayVerifier verifier,
    EntitlementRepository repository,
    HybridCache cache,
    CancellationToken cancellationToken) =>
{
    var verification = await verifier.VerifyAndAcknowledgeAsync(
        request.ProductId, request.PurchaseToken, cancellationToken);

    // NEVER grant on PENDING — the payment can still fail. Tell the client to re-check later.
    if (verification.Status == PurchaseStatus.Pending)
        return Results.Accepted(value: new { status = "pending" });

    if (!verification.GrantsEntitlement)
        return Results.BadRequest(new { status = verification.Status.ToString().ToLowerInvariant() });

    var granted = await repository.TryGrantAsync(
        new EntitlementGrant(request.UserId, request.ProductId, request.PurchaseToken, verification.OrderId),
        cancellationToken);

    // Invalidate the cached read so the game sees ads_removed immediately on its next check.
    await cache.RemoveAsync($"ent:{request.UserId}", cancellationToken);

    // duplicate == true means this token was already recorded (Play retry / double-tap): idempotent OK.
    return Results.Ok(new { status = "granted", adsRemoved = true, duplicate = !granted });
});

// Called by the game on launch. Server-authoritative + cross-device.
app.MapGet("/entitlements/{userId}", async (
    string userId,
    EntitlementRepository repository,
    HybridCache cache,
    CancellationToken cancellationToken) =>
{
    // Allocation-free overload: pass state explicitly so the factory lambda doesn't capture.
    var adsRemoved = await cache.GetOrCreateAsync(
        $"ent:{userId}",
        (repository, userId),
        static async (state, ct) => await state.repository.IsAdsRemovedAsync(state.userId, ct),
        cancellationToken: cancellationToken);

    return Results.Ok(new { userId, adsRemoved });
});

app.Run();

internal sealed record VerifyPurchaseRequest(string UserId, string ProductId, string PurchaseToken);
