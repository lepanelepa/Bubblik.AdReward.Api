using Dapper;
using Npgsql;

namespace Puzyrik.AdReward.Api.Data;

public static class DbInitializer
{
    // Idempotent — safe to run on every startup. Phase 2 will add `ad_events` and `ad_config`
    // tables here (or move to a real migration tool). reward_grants.transaction_id is the
    // PRIMARY KEY on purpose: it's the idempotency anchor for AdMob SSV callbacks, exactly
    // like the dedup key in a payment reconciliation flow.
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(Schema);
    }

    // C# 11 raw string literal — no escaping noise.
    private const string Schema =
        """
        CREATE TABLE IF NOT EXISTS reward_grants (
            transaction_id  TEXT        PRIMARY KEY,
            user_id         TEXT        NOT NULL,
            ad_unit         TEXT        NOT NULL,
            reward_item     TEXT        NOT NULL,
            reward_amount   INTEGER     NOT NULL,
            granted_at      TIMESTAMPTZ NOT NULL DEFAULT now()
        );
        """;
}
