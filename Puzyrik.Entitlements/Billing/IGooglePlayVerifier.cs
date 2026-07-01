namespace Puzyrik.Entitlements.Billing;

// Maps Google Play's numeric purchaseState into something the endpoint can reason about.
public enum PurchaseStatus
{
    Purchased, // purchaseState == 0  -> grant entitlement
    Pending,   // purchaseState == 2  -> DO NOT grant yet (payment may still fail)
    Canceled,  // purchaseState == 1  -> refunded/canceled
    Invalid    // token rejected by Google / unknown state
}

public sealed record PurchaseVerification(
    PurchaseStatus Status,
    string? OrderId,            // may be null! promo-code purchases have no orderId
    bool AlreadyAcknowledged)
{
    public bool GrantsEntitlement => Status == PurchaseStatus.Purchased;
}

// Behind an interface so the endpoint never touches Google types directly — makes the
// verify flow unit-testable with a fake, and lets dev run without Play Console credentials.
public interface IGooglePlayVerifier
{
    Task<PurchaseVerification> VerifyAndAcknowledgeAsync(
        string productId,
        string purchaseToken,
        CancellationToken cancellationToken);
}
