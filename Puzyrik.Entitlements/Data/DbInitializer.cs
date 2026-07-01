using Dapper;
using Npgsql;

namespace Puzyrik.Entitlements.Data;

public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(Schema);
    }

    // purchase_token is the PRIMARY KEY (idempotency anchor). revoked_at supports Phase 2
    // refund/void handling via the Voided Purchases API / RTDN.
    private const string Schema =
        """
        CREATE TABLE IF NOT EXISTS entitlements (
            purchase_token TEXT        PRIMARY KEY,
            user_id        TEXT        NOT NULL,
            product_id     TEXT        NOT NULL,
            order_id       TEXT,
            granted_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
            revoked_at     TIMESTAMPTZ
        );

        CREATE INDEX IF NOT EXISTS ix_entitlements_user
            ON entitlements (user_id);
        """;
}
