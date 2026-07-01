# Puzyrik.Entitlements — Phase 1 ($1 remove-ads)

Second service in the same Aspire AppHost. Verifies a Google Play purchase of the one-time
non-consumable remove-ads product and records a server-authoritative entitlement, so "ads
removed" survives reinstalls and follows the user across devices. The shared scaffold steps
(prereqs, template, dashboard) are in the main `README.md`.

## Add the project

```bash
# from the solution root
dotnet new webapi -o Puzyrik.Entitlements --use-minimal-apis
dotnet sln add Puzyrik.Entitlements

# reference ServiceDefaults so AddServiceDefaults()/MapDefaultEndpoints() are available
dotnet add Puzyrik.Entitlements reference Puzyrik.AdReward.ServiceDefaults
```

Then in the **AppHost** add a project reference to it (so `Projects.Puzyrik_Entitlements`
resolves), and use the updated `AppHost.cs` from this bundle.

## Packages (in Puzyrik.Entitlements)

```bash
dotnet add package Aspire.Npgsql
dotnet add package Dapper
dotnet add package Google.Apis.AndroidPublisher.v3      # official Play Developer API client
dotnet add package Microsoft.Extensions.Caching.Hybrid  # HybridCache
```

## Drop-in files

| Bundle file | Goes to |
|---|---|
| `src/Entitlements/Program.cs` | replace generated `Program.cs` |
| `src/Entitlements/Billing/*.cs` | API project |
| `src/Entitlements/Data/*.cs` | API project |
| `src/Entitlements/Options/GooglePlayOptions.cs` | API project |
| `src/AppHost/AppHost.cs` | replace AppHost's file (now wires both services) |

One-line edit in the existing test: `CreateHttpClient("api")` → `CreateHttpClient("rewards-api")`.

## Run without Play Console (dev)

In Development the service uses `FakeGooglePlayVerifier`, so it runs end-to-end with no
credentials. From the dashboard, find the `entitlements-api` port and:

```bash
# grant (any non-empty, non-"pending_" token is treated as Purchased by the fake)
curl -X POST http://localhost:<port>/purchases/google/verify \
  -H 'Content-Type: application/json' \
  -d '{"userId":"u_42","productId":"remove_ads","purchaseToken":"tok_abc123"}'
# -> {"status":"granted","adsRemoved":true,"duplicate":false}

# same token again -> idempotent no-op
# -> {"status":"granted","adsRemoved":true,"duplicate":true}

# pending payment path
curl -X POST .../purchases/google/verify -H 'Content-Type: application/json' \
  -d '{"userId":"u_43","productId":"remove_ads","purchaseToken":"pending_x"}'
# -> 202 {"status":"pending"}

# the game's launch check
curl http://localhost:<port>/entitlements/u_42
# -> {"userId":"u_42","adsRemoved":true}
```

## Wire real Google Play (when ready)

1. Google Cloud project → enable **Google Play Android Developer API**.
2. Create a **service account**, download its JSON key.
3. In **Play Console → Users and permissions**, invite the service-account email and grant
   **View financial data** (without this you get 401s).
4. Create the non-consumable product (e.g. `remove_ads`) in the Play Console.
5. Configure (use user-secrets locally, a mounted secret in GKE — never commit the JSON):

```json
{
  "GooglePlay": {
    "PackageName": "me.puzyrik.baloonx",
    "RemoveAdsProductId": "remove_ads",
    "ServiceAccountJsonPath": "/secrets/play-service-account.json"
  }
}
```

Outside Development the service switches to `GooglePlayVerifier`, which calls
`purchases.products.get`, checks `purchaseState`, and acknowledges the purchase.

## Correctness rules baked in (and worth saying out loud at interview)

- **Idempotency on `purchaseToken`, never `orderId`** — promo-code purchases have no orderId.
- **Never grant on `PENDING`** — payment may still fail; the endpoint returns 202.
- **Acknowledge server-side** — unacknowledged non-consumables auto-refund after ~3 days.
- **Server-authoritative read** — the game trusts `GET /entitlements/{userId}`, not a local flag.

## Next (Phase 2)

Real-time Developer Notifications (RTDN) over Pub/Sub + the Voided Purchases API to set
`revoked_at` on refunds/chargebacks — reuse the Channels ingestion pattern from the rewards
service for the notification webhook.
