// Puzyrik.AdReward.Api — Program.cs
// REPLACES the template-generated Program.cs in the API project.

using Puzyrik.AdReward.Api.Data;
using Puzyrik.AdReward.Api.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults (comes from the generated ServiceDefaults project):
//   - OpenTelemetry wired for ASP.NET Core, HttpClient and the .NET runtime
//   - default health-check endpoints
//   - service discovery + standard HttpClient resilience handlers
// This single call is most of "modern .NET hosting" you missed in Unity.
builder.AddServiceDefaults();

// PostgreSQL client integration (Aspire.Npgsql). Registers an NpgsqlDataSource in DI and
// ALSO adds its own connection health check + telemetry automatically. "addb" must match
// the database resource name declared in the AppHost.
builder.AddNpgsqlDataSource(connectionName: "addb");

// Our data layer. RewardRepository is the idempotent reward-granting repo (Dapper, not EF) —
// the foundation for Phase 2's SSV reward grants.
builder.Services.AddScoped<RewardRepository>();

// A second, explicit health check written by hand so you see the IHealthCheck pattern.
// (The Npgsql integration already adds a connection check; this one actually runs a query.)
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseQueryHealthCheck>("db-query", tags: ["ready"]);

var app = builder.Build();

// Phase 1 startup task: create the schema if it doesn't exist (idempotent). In a bigger
// system this becomes a proper migration step; for now it keeps the loop tight.
await app.Services.InitializeDatabaseAsync();

// /health (all checks) and /alive (liveness) from ServiceDefaults — used by Aspire & k8s.
app.MapDefaultEndpoints();

// Canonical k8s-style readiness path that includes the DB query check above.
app.MapHealthChecks("/healthz");

app.MapGet("/", () => "Puzyrik.AdReward is up. Try /healthz");

app.Run();
