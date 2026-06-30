using System.Net;

namespace Puzyrik.AdReward.Tests;

// Aspire integration test: boots the WHOLE app model (Postgres container + API) and probes
// the real /healthz endpoint. Requires Docker to be running, because it actually starts
// Postgres. This is the canonical Aspire.Hosting.Testing pattern.
public class HealthEndpointTests
{
    [Fact]
    public async Task Healthz_returns_ok_when_database_is_reachable()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Puzyrik_AdReward_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Wait until the API resource reports healthy before probing it.
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api");

        var httpClient = app.CreateHttpClient("api");
        var response = await httpClient.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
