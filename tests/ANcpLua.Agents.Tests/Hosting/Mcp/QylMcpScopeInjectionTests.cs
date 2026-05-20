using System.ComponentModel;
using System.Text.Json;
using ANcpLua.Agents.Mcp;
using ANcpLua.Agents.Mcp.Hosting;
using ANcpLua.Agents.Mcp.Hosting.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;

namespace ANcpLua.Agents.Tests.Hosting.Mcp;

public sealed class QylMcpScopeInjectionTests
{
    [Fact]
    public async Task WithQylScopeInjection_ScopeAndInjectorRegistered_RewritesArguments()
    {
        await using var fixture = await ScopeInjectionFixture.CreateAsync(services =>
        {
            services.AddSingleton(new TestScope { Constraint = "tenant-42" });
            services.AddSingleton<IQylConstraintInjector<TestScope>, TestInjector>();
        });

        ScopeProbeTool.LastConstraint = null;

        var result = await fixture.Client.CallToolAsync(
            "scope_probe",
            arguments: null,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsError.Should().NotBeTrue();
        ScopeProbeTool.LastConstraint.Should().Be("tenant-42");
    }

    [Fact]
    public async Task WithQylScopeInjection_ScopeNotRegistered_PassesThrough()
    {
        await using var fixture = await ScopeInjectionFixture.CreateAsync(configureServices: null);

        ScopeProbeTool.LastConstraint = "untouched";

        var result = await fixture.Client.CallToolAsync(
            "scope_probe",
            arguments: null,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsError.Should().NotBeTrue();
        ScopeProbeTool.LastConstraint.Should().BeNull();
    }

    [Fact]
    public async Task WithQylScopeInjection_ScopeRegistered_InjectorMissing_FailsTheCall()
    {
        await using var fixture = await ScopeInjectionFixture.CreateAsync(services =>
        {
            services.AddSingleton(new TestScope { Constraint = "tenant-42" });
        });

        // The filter throws InvalidOperationException server-side when the scope is registered
        // without a matching IQylConstraintInjector. The MCP SDK 1.3.0 converts that throw into
        // a CallToolResult with IsError=true rather than propagating as a client-side exception,
        // so the contract is "the call fails" — the wire shape is the SDK's choice.
        var result = await fixture.Client.CallToolAsync(
            "scope_probe",
            arguments: null,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        var text = result.Content
            .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .FirstOrDefault()?.Text ?? string.Empty;
        text.Should().Contain(nameof(IQylConstraintInjector<TestScope>));
    }

    private sealed class TestScope
    {
        public required string Constraint { get; init; }
    }

    private sealed class TestInjector : IQylConstraintInjector<TestScope>
    {
        public IDictionary<string, JsonElement>? Inject(
            IDictionary<string, JsonElement>? arguments,
            TestScope scope)
        {
            var dict = arguments ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            dict["constraint"] = JsonSerializer.SerializeToElement(scope.Constraint);
            return dict;
        }
    }

    [McpServerToolType]
    internal sealed class ScopeProbeTool
    {
        public static string? LastConstraint;

        [McpServerTool(Name = "scope_probe")]
        [Description("Records the value of the 'constraint' argument it was called with.")]
        public static string Probe(string? constraint = null)
        {
            LastConstraint = constraint;
            return constraint ?? "<null>";
        }
    }

    private sealed class ScopeInjectionFixture : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly HttpClient _httpClient;
        private readonly HttpClientTransport _transport;

        public McpClient Client { get; }

        private ScopeInjectionFixture(WebApplication app, HttpClient httpClient, HttpClientTransport transport, McpClient client)
        {
            _app = app;
            _httpClient = httpClient;
            _transport = transport;
            Client = client;
        }

        public static async Task<ScopeInjectionFixture> CreateAsync(Action<IServiceCollection>? configureServices)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

            configureServices?.Invoke(builder.Services);

            builder.Services
                .AddMcpServer()
                .WithHttpTransport(o => o.Stateless = true)
                .WithTools<ScopeProbeTool>()
                .WithQylScopeInjection<TestScope>();

            var app = builder.Build();
            app.MapQylMcp(mapHealthEndpoints: false);
            await app.StartAsync();

            var httpClient = app.GetTestClient();
            httpClient.BaseAddress = new Uri("http://localhost/");

            var transport = new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri("http://localhost/mcp") },
                httpClient,
                loggerFactory: null,
                ownsHttpClient: false);
            var client = await McpClient.CreateAsync(transport);

            return new ScopeInjectionFixture(app, httpClient, transport, client);
        }

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            await _transport.DisposeAsync();
            _httpClient.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
