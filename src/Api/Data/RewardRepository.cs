using Dapper;
using Npgsql;

namespace Puzyrik.AdReward.Api.Data;

// The reward record AdMob's SSV callback will map onto in Phase 2.
public sealed record RewardGrant(
    string TransactionId,
    string UserId,
    string AdUnit,
    string RewardItem,
    int RewardAmount);

// Dapper, deliberately — thin mapper over hand-written SQL, full control over the statement.
// (EF Core stays out of the hot path; this is the "close to SQL" stance, in .NET.)
public sealed class RewardRepository(NpgsqlDataSource dataSource)
{
    // Returns true if this call actually granted the reward, false if it was a duplicate
    // (AdMob retries SSV callbacks up to 5 times — duplicates MUST be a no-op). The
    // ON CONFLICT DO NOTHING + PRIMARY KEY(transaction_id) makes the grant idempotent at
    // the database level, so concurrent retries can't double-credit Пузырик.
    public async Task<bool> TryGrantAsync(RewardGrant grant, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO reward_grants
                (transaction_id, user_id, ad_unit, reward_item, reward_amount)
            VALUES
                (@TransactionId, @UserId, @AdUnit, @RewardItem, @RewardAmount)
            ON CONFLICT (transaction_id) DO NOTHING;
            """,
            grant,
            cancellationToken: cancellationToken));

        return rowsAffected == 1;
    }
}
