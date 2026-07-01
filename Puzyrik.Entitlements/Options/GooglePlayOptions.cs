namespace Puzyrik.Entitlements.Options;

// Bound from configuration section "GooglePlay" (appsettings / user-secrets / env).
public sealed class GooglePlayOptions
{
    public const string SectionName = "GooglePlay";

    // e.g. "me.puzyrik.baloonx"
    public string PackageName { get; set; } = "";

    // The non-consumable product id you create in the Play Console for the $1 remove-ads SKU.
    public string RemoveAdsProductId { get; set; } = "remove_ads";

    // Path to the service-account JSON. Keep it OUT of source control; use user-secrets locally
    // and a mounted secret / Secret Manager in GKE.
    public string ServiceAccountJsonPath { get; set; } = "";
}
