# Puzyrik.AdReward — Phase 1

Aspire-orchestrated .NET 10 backend for Пузырик. **Phase 1 goal:** feel the modern hosting
model — AppHost + DI + Aspire dashboard + Postgres via Dapper + a real `/healthz` — running
locally with one `dotnet run`. The AdMob SSV verification, the Channels event pipeline and the
remote-config endpoint come in later phases; the idempotent `reward_grants` table is already
in place so Phase 2 drops straight in.

## Why "template, then drop in these files"

Aspire's project/SDK wiring is version-sensitive and changes between releases. Rather than hand
you `.csproj` files pinned to versions that may not match your install, you generate the skeleton
from the official template (correct wiring guaranteed for *your* Aspire version), then replace a
handful of files with the ones in this bundle. The files here are the parts you'd actually write
by hand anyway; the template handles the boilerplate.

## Prerequisites

- **.NET 10 SDK** (LTS, supported through Nov 2028)
- **Docker** running (Aspire starts Postgres as a container; the integration test needs it too)
- **Aspire templates**: `dotnet new install Aspire.ProjectTemplates`
  (or the `aspire` CLI: `dotnet tool install -g Aspire.Cli`, then `aspire new`)

## 1. Scaffold the skeleton

```bash
# Creates AppHost + ServiceDefaults + an API + a test project, wired together correctly.
dotnet new aspire-starter -o Puzyrik.AdReward
cd Puzyrik.AdReward
```

This gives you projects named like `Puzyrik.AdReward.AppHost`, `Puzyrik.AdReward.ServiceDefaults`,
`Puzyrik.AdReward.ApiService` (or `.Web`), and `Puzyrik.AdReward.Tests`. Rename the API project to
`Puzyrik.AdReward.Api` so the `Projects.Puzyrik_AdReward_Api` reference in `AppHost.cs` resolves
(Aspire turns dots into underscores for the generated `Projects.*` type). If you keep the default
name, just adjust that one identifier.

## 2. Add the two packages the API needs

From the API project folder:

```bash
dotnet add package Aspire.Npgsql      # NpgsqlDataSource in DI + health check + telemetry
dotnet add package Dapper             # thin SQL mapper (no EF on the hot path)
```

And in the **AppHost** project:

```bash
dotnet add package Aspire.Hosting.PostgreSQL
```

For the **test** project (the template usually adds it; confirm it's there):

```bash
dotnet add package Aspire.Hosting.Testing
```

> Let `dotnet add package` resolve the latest compatible versions for your Aspire install
> (13.x at the time of writing) rather than pinning by hand.

## 3. Drop in the files from this bundle

| Bundle file | Goes to |
|---|---|
| `src/AppHost/AppHost.cs` | AppHost project — replace its `AppHost.cs` / `Program.cs` |
| `src/Api/Program.cs` | API project — replace `Program.cs` |
| `src/Api/HealthChecks/DatabaseQueryHealthCheck.cs` | API project |
| `src/Api/Data/DbInitializer.cs` | API project |
| `src/Api/Data/RewardRepository.cs` | API project |
| `tests/HealthEndpointTests.cs` | Tests project |
| `db/001_init.sql` | reference only (schema is auto-applied at startup) |

Adjust the `namespace` lines if your projects use a different root namespace. Leave the generated
`ServiceDefaults` project untouched — `AddServiceDefaults()` and `MapDefaultEndpoints()` come from it.

## 4. Run it

```bash
dotnet run --project Puzyrik.AdReward.AppHost
```

The Aspire **dashboard** opens automatically. You should see:

- a **postgres** resource and an **api** resource, both going healthy (api waits for postgres);
- **Structured logs**, **Traces**, and **Metrics** tabs populated with no extra setup — that's
  OpenTelemetry from ServiceDefaults. Hit the endpoints a few times and watch traces appear.

Probe the API (port is shown in the dashboard, or follow the `api` resource link):

```bash
curl http://localhost:<port>/healthz     # 200 once the DB query check passes
curl http://localhost:<port>/            # "Puzyrik.AdReward is up. Try /healthz"
```

Run the integration test (Docker must be up):

```bash
dotnet test
```

## What each piece is teaching you

- **AppHost.cs** — the orchestration model. No docker-compose, no hand-written connection strings.
- **AddServiceDefaults()** — OTel + health + service discovery + resilient HttpClient in one call.
- **AddNpgsqlDataSource("addb")** — Aspire client integration: DI + health check + telemetry.
- **DatabaseQueryHealthCheck** — the `IHealthCheck` contract, primary-constructor DI, `CancellationToken`.
- **RewardRepository** — Dapper + `ON CONFLICT DO NOTHING` idempotency (your payment-gateway instinct, in .NET).
- **HealthEndpointTests** — `Aspire.Hosting.Testing`: boot the whole app model in a test.

## Next (Phase 2 preview)

Add `GET /ssv/admob`: read the **raw** query string (order must not change — it's what the
signature covers), verify the ECDSA signature with `System.Security.Cryptography.ECDsa` against a
public key fetched from `https://www.gstatic.com/admob/reward/verifier-keys.json` (cache ≤ 24h via
`HybridCache`), then call `RewardRepository.TryGrantAsync`. No Tink needed — .NET verifies ECDSA natively.
