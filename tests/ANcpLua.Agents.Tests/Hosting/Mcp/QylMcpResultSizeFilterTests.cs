using System.ComponentModel;
using System.Text.Json.Nodes;
using ANcpLua.Agents.Mcp.Hosting;
using ANcpLua.Agents.Mcp.Hosting.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ANcpLua.Agents.Tests.Hosting.Mcp;

public sealed class QylMcpResultSizeFilterTests
{
    [Fact]
    public async Task WithAnthropicResultSizeMeta_ResultOverThreshold_SetsMetaKey()
    {
        await using var fixture = await McpFilterFixture.CreateAsync(b => b.WithAnthropicResultSizeMeta(thresholdChars: 100));

        var result = await fixture.Client.CallToolAsync(
            "long_response",
            new Dictionary<string, object?> { ["lengthChars"] = 250 },
            cancellationToken: TestContext.Current.CancellationToken);

        result.Meta.Should().NotBeNull();
        result.Meta!.Should().ContainKey("anthropic/maxResultSizeChars");
        result.Meta["anthropic/maxResultSizeChars"]!.GetValue<int>().Should().Be(250);
    }

    [Fact]
    public async Task WithAnthropicResultSizeMeta_ResultUnderThreshold_DoesNotSetMetaKey()
    {
        await using var fixture = await McpFilterFixture.CreateAsync(b => b.WithAnthropicResultSizeMeta(thresholdChars: 100));

        var result = await fixture.Client.CallToolAsync(
            "long_response",
            new Dictionary<string, object?> { ["lengthChars"] = 50 },
            cancellationToken: TestContext.Current.CancellationToken);

        if (result.Meta is not null)
            result.Meta.Should().NotContainKey("anthropic/maxResultSizeChars");
    }

    [Fact]
    public async Task WithAnthropicResultSizeMeta_PreservesPreExistingMeta()
    {
        await using var fixture = await McpFilterFixture.CreateAsync(b => b.WithAnthropicResultSizeMeta(thresholdChars: 100));

        var result = await fixture.Client.CallToolAsync(
            "meta_seeded",
            new Dictionary<string, object?> { ["lengthChars"] = 250 },
            cancellationToken: TestContext.Current.CancellationToken);

        result.Meta.Should().NotBeNull();
        result.Meta!.Should().ContainKey("anthropic/maxResultSizeChars");
        result.Meta["anthropic/maxResultSizeChars"]!.GetValue<int>().Should().Be(250);
        result.Meta.Should().ContainKey("qyl/sentinel");
        result.Meta["qyl/sentinel"]!.GetValue<string>().Should().Be("preserved");
    }
}

[McpServerToolType]
internal sealed class ResultSizeFilterTestTools
{
    [McpServerTool(Name = "long_response")]
    [Description("Returns a string of the specified length.")]
    public static string LongResponse([Description("Length in characters.")] int lengthChars)
    {
        return new string('x', lengthChars);
    }

    [McpServerTool(Name = "meta_seeded")]
    [Description("Returns a long response whose CallToolResult already carries a sentinel meta key.")]
    public static CallToolResult MetaSeeded([Description("Length in characters.")] int lengthChars)
    {
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = new string('x', lengthChars) }],
            Meta = new JsonObject { ["qyl/sentinel"] = "preserved" }
        };
    }
}

internal sealed class McpFilterFixture : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly HttpClient _httpClient;
    private readonly HttpClientTransport _transport;

    public McpClient Client { get; }

    private McpFilterFixture(WebApplication app, HttpClient httpClient, HttpClientTransport transport, McpClient client)
    {
        _app = app;
        _httpClient = httpClient;
        _transport = transport;
        Client = client;
    }

    public static async Task<McpFilterFixture> CreateAsync(Action<IMcpServerBuilder> configureBuilder)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var mcpBuilder = builder.Services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<ResultSizeFilterTestTools>();

        configureBuilder(mcpBuilder);

        var app = builder.Build();
        app.MapQylMcp(mapHealthEndpoints: false);
        await app.StartAsync();

        var httpClient = app.GetTestClient();
        httpClient.BaseAddress = new Uri("http://localhost/");
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri("http://localhost/mcp") },
            httpClient,
            loggerFactory: null!,
            ownsHttpClient: false);
        var client = await McpClient.CreateAsync(transport);

        return new McpFilterFixture(app, httpClient, transport, client);
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
