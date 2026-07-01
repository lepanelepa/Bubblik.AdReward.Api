using Google;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using Puzyrik.Entitlements.Options;

namespace Puzyrik.Entitlements.Billing;

// Verifies a purchase against Google Play using the official Google.Apis.AndroidPublisher.v3
// client (no hand-rolled REST). Authenticates with a service account that you grant
// "View financial data" permission to in the Play Console.
public sealed class GooglePlayVerifier : IGooglePlayVerifier
{
    private readonly GooglePlayOptions _options;
    private readonly ILogger<GooglePlayVerifier> _logger;

    // The Google client is built lazily on first use: credential loading is async now, which
    // can't happen in a constructor. SemaphoreSlim guards against two concurrent first-calls
    // both trying to build it.
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private AndroidPublisherService? _service;

    public GooglePlayVerifier(IOptions<GooglePlayOptions> options, ILogger<GooglePlayVerifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private async Task<AndroidPublisherService> GetServiceAsync(CancellationToken cancellationToken)
    {
        if (_service is not null)
            return _service;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_service is not null)
                return _service;

var credential = (await CredentialFactory
                    .FromFileAsync(_options.ServiceAccountJsonPath, "service_account", cancellationToken))
                .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

            _service = new AndroidPublisherService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Puzyrik.Entitlements"
            });

            return _service;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<PurchaseVerification> VerifyAndAcknowledgeAsync(
        string productId,
        string purchaseToken,
        CancellationToken cancellationToken)
    {
        var service = await GetServiceAsync(cancellationToken);

        ProductPurchase purchase;
        try
        {
            // GET .../purchases/products/{productId}/tokens/{token}
            purchase = await service.Purchases.Products
                .Get(_options.PackageName, productId, purchaseToken)
                .ExecuteAsync(cancellationToken);
        }
        catch (GoogleApiException ex)
        {
            // Bad/forged/expired token -> Google returns 4xx. Treat as invalid, don't throw 500.
            _logger.LogWarning(ex, "Google Play rejected purchase token for product {ProductId}", productId);
            return new PurchaseVerification(PurchaseStatus.Invalid, OrderId: null, AlreadyAcknowledged: false);
        }

        // purchaseState: 0 = Purchased, 1 = Canceled, 2 = Pending
        var status = purchase.PurchaseState switch
        {
            0 => PurchaseStatus.Purchased,
            2 => PurchaseStatus.Pending,
            1 => PurchaseStatus.Canceled,
            _ => PurchaseStatus.Invalid
        };

        // acknowledgementState: 0 = yet to acknowledge, 1 = acknowledged
        var alreadyAcknowledged = purchase.AcknowledgementState == 1;

        // Acknowledge a genuine purchase server-side, or Google auto-refunds after ~3 days.
        if (status == PurchaseStatus.Purchased && !alreadyAcknowledged)
        {
            await service.Purchases.Products
                .Acknowledge(new ProductPurchasesAcknowledgeRequest(), _options.PackageName, productId, purchaseToken)
                .ExecuteAsync(cancellationToken);
        }

        return new PurchaseVerification(status, purchase.OrderId, alreadyAcknowledged);
    }
}