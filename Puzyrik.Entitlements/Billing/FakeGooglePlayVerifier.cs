namespace Puzyrik.Entitlements.Billing;

// Dev/test stand-in so the whole service runs end-to-end without real Play Console setup.
// Convention for poking it locally:
//   - empty token        -> Invalid
//   - token "pending_..." -> Pending (exercises the "don't grant yet" path)
//   - anything else       -> Purchased
public sealed class FakeGooglePlayVerifier : IGooglePlayVerifier
{
    public Task<PurchaseVerification> VerifyAndAcknowledgeAsync(
        string productId,
        string purchaseToken,
        CancellationToken cancellationToken)
    {
        PurchaseVerification result;

        if (string.IsNullOrWhiteSpace(purchaseToken))
        {
            result = new PurchaseVerification(PurchaseStatus.Invalid, null, false);
        }
        else if (purchaseToken.StartsWith("pending_", StringComparison.Ordinal))
        {
            result = new PurchaseVerification(PurchaseStatus.Pending, null, false);
        }
        else
        {
            var stub = purchaseToken[..Math.Min(8, purchaseToken.Length)];
            result = new PurchaseVerification(PurchaseStatus.Purchased, $"GPA.FAKE-{stub}", false);
        }

        return Task.FromResult(result);
    }
}
