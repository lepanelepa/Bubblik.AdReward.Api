using Dapper;
using Npgsql;

namespace Puzyrik.Entitlements.Data;

public sealed record EntitlementGrant(
    string UserId,
    string ProductId,
    string PurchaseToken,
    string? OrderId);

public sealed class EntitlementRepository(NpgsqlDataSource dataSource)
{
    // Idempotent on PURCHASE_TOKEN, deliberately — orderId can be null (promo codes), so it
    // can't be the dedup key. Returns false on a duplicate token (retry / double-tap) = no-op.
    public async Task<bool> TryGrantAsync(EntitlementGrant grant, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO entitlements (purchase_token, user_id, product_id, order_id)
            VALUES (@PurchaseToken, @UserId, @ProductId, @OrderId)
            ON CONFLICT (purchase_token) DO NOTHING;
            """,
            grant,
            cancellationToken: cancellationToken));

        return rowsAffected == 1;
    }

    // The hot read path the game hits on launch. revoked_at IS NULL respects Phase 2 refunds.
    public async Task<bool> IsAdsRemovedAsync(string userId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            """
            SELECT EXISTS (
                SELECT 1 FROM entitlements
                WHERE user_id = @userId
                  AND product_id = 'remove_ads'
                  AND revoked_at IS NULL
            );
            """,
            new { userId },
            cancellationToken: cancellationToken));
    }
}
