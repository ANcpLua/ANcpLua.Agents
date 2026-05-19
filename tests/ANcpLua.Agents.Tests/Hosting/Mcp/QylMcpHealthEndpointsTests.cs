using ANcpLua.Agents.Mcp.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ANcpLua.Agents.Tests.Hosting.Mcp;

public sealed class QylMcpHealthEndpointsTests
{
    [Fact]
    public async Task MapQylMcp_DefaultMapHealthEndpoints_AliveReturnsOnlyLiveTaggedChecks()
    {
        await using var fixture = await HealthEndpointFixture.CreateAsync(mapHealthEndpoints: true);

        using var response = await fixture.HttpClient.GetAsync(
            "/alive",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task MapQylMcp_DefaultMapHealthEndpoints_HealthReturnsReadyTaggedChecks()
    {
        await using var fixture = await HealthEndpointFixture.CreateAsync(mapHealthEndpoints: true);

        using var response = await fixture.HttpClient.GetAsync(
            "/health",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task MapQylMcp_LiveAndReadyChecksReturnIndependentResults()
    {
        await using var fixture = await HealthEndpointFixture.CreateAsync(
            mapHealthEndpoints: true,
            registerChecks: hc => hc
                .AddCheck("live-check", static () => HealthCheckResult.Healthy("alive-only"), tags: ["live"])
                .AddCheck("ready-check", static () => HealthCheckResult.Unhealthy("waiting-on-deps"), tags: ["ready"]));

        using var aliveResponse = await fixture.HttpClient.GetAsync(
            "/alive",
            TestContext.Current.CancellationToken);
        using var healthResponse = await fixture.HttpClient.GetAsync(
            "/health",
            TestContext.Current.CancellationToken);

        aliveResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        healthResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task MapQylMcp_MapHealthEndpointsFalse_AliveReturns404()
    {
        await using var fixture = await HealthEndpointFixture.CreateAsync(mapHealthEndpoints: false);

        using var response = await fixture.HttpClient.GetAsync(
            "/alive",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    private sealed class HealthEndpointFixture : IAsyncDisposable
    {
        private readonly WebApplication _app;
        public HttpClient HttpClient { get; }

        private HealthEndpointFixture(WebApplication app, HttpClient httpClient)
        {
            _app = app;
            HttpClient = httpClient;
        }

        public static async Task<HealthEndpointFixture> CreateAsync(
            bool mapHealthEndpoints,
            Action<IHealthChecksBuilder>? registerChecks = null)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

            var healthBuilder = builder.Services.AddHealthChecks();
            if (registerChecks is not null)
                registerChecks(healthBuilder);
            else
                healthBuilder
                    .AddCheck("self", static () => HealthCheckResult.Healthy(), tags: ["live"])
                    .AddCheck("collector", static () => HealthCheckResult.Healthy(), tags: ["ready"]);

            builder.Services
                .AddMcpServer()
                .WithHttpTransport(o => o.Stateless = true);

            var app = builder.Build();
            app.MapQylMcp(mapHealthEndpoints: mapHealthEndpoints);

            await app.StartAsync();

            var httpClient = app.GetTestClient();
            httpClient.BaseAddress = new Uri("http://localhost/");

            return new HealthEndpointFixture(app, httpClient);
        }

        public async ValueTask DisposeAsync()
        {
            HttpClient.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
