using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Puzyrik.AdReward.Api.HealthChecks;

// Demonstrates the IHealthCheck contract end-to-end: resolve the Aspire-registered
// NpgsqlDataSource from DI (primary-constructor injection, C# 12), open a connection,
// run a trivial query with Dapper, and map the result to a health status.
// Cancellation token is threaded through — your Go `context.Context` muscle memory applies.
public sealed class DatabaseQueryHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

            var result = await connection.QuerySingleAsync<int>(
                new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));

            return result == 1
                ? HealthCheckResult.Healthy("Database reachable and answering queries")
                : HealthCheckResult.Unhealthy($"Unexpected query result: {result}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database query failed", ex);
        }
    }
}
