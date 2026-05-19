using System.ComponentModel;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ANcpLua.Agents.Mcp.Hosting;
using ANcpLua.Agents.Mcp.Hosting.Filters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;

namespace ANcpLua.Agents.Tests.Hosting.Mcp;

public sealed class QylMcpAdminFilterTests
{
    [Fact]
    public async Task WithQylAdminFilter_AdminToolAndMissingRole_ReturnsDenial()
    {
        AdminFilterTestTools.DeleteEverythingInvocations = 0;

        await using var fixture = await AdminFilterFixture.CreateAsync(
            roles: ["qyl:reader"],
            configure: o =>
            {
                o.RequiredRole = "qyl:admin";
                o.AdminToolNames = new HashSet<string>(StringComparer.Ordinal) { "delete_everything" };
            });

        var result = await fixture.Client.CallToolAsync(
            "delete_everything",
            arguments: null,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        AdminFilterFixture.GetText(result)
            .Should().Contain("Access denied")
            .And.Contain("delete_everything")
            .And.Contain("qyl:admin");

        AdminFilterTestTools.DeleteEverythingInvocations.Should().Be(0);
    }

    [Fact]
    public async Task WithQylAdminFilter_AdminToolAndRolePresent_AllowsCall()
    {
        AdminFilterTestTools.DeleteEverythingInvocations = 0;

        await using var fixture = await AdminFilterFixture.CreateAsync(
            roles: ["qyl:admin"],
            configure: o =>
            {
                o.RequiredRole = "qyl:admin";
                o.AdminToolNames = new HashSet<string>(StringComparer.Ordinal) { "delete_everything" };
            });

        var result = await fixture.Client.CallToolAsync(
            "delete_everything",
            arguments: null,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsError.Should().NotBe(true);
        AdminFilterFixture.GetText(result).Should().Be("deleted");
        AdminFilterTestTools.DeleteEverythingInvocations.Should().Be(1);
    }

    [Fact]
    public async Task WithQylAdminFilter_NonAdminTool_BypassesRoleCheck()
    {
        AdminFilterTestTools.SafeReadInvocations = 0;

        await using var fixture = await AdminFilterFixture.CreateAsync(
            roles: [],
            configure: o =>
            {
                o.RequiredRole = "qyl:admin";
                o.AdminToolNames = new HashSet<string>(StringComparer.Ordinal) { "delete_everything" };
            });

        var result = await fixture.Client.CallToolAsync(
            "safe_read",
            arguments: null,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsError.Should().NotBe(true);
        AdminFilterFixture.GetText(result).Should().Be("ok");
        AdminFilterTestTools.SafeReadInvocations.Should().Be(1);
    }
}

[McpServerToolType]
internal sealed class AdminFilterTestTools
{
    public static int DeleteEverythingInvocations;
    public static int SafeReadInvocations;

    [McpServerTool(Name = "delete_everything")]
    [Description("Destructive admin-gated test tool.")]
    public static string DeleteEverything()
    {
        Interlocked.Increment(ref DeleteEverythingInvocations);
        return "deleted";
    }

    [McpServerTool(Name = "safe_read")]
    [Description("Non-admin test tool.")]
    public static string SafeRead()
    {
        Interlocked.Increment(ref SafeReadInvocations);
        return "ok";
    }
}

internal sealed class TestAuthOptions : AuthenticationSchemeOptions
{
    public IReadOnlyCollection<string> Roles { get; set; } = [];
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<TestAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = Options.Roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-user"));
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class AdminFilterFixture : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly HttpClient _httpClient;
    private readonly HttpClientTransport _transport;

    public McpClient Client { get; }

    private AdminFilterFixture(WebApplication app, HttpClient httpClient, HttpClientTransport transport, McpClient client)
    {
        _app = app;
        _httpClient = httpClient;
        _transport = transport;
        Client = client;
    }

    public static async Task<AdminFilterFixture> CreateAsync(
        IReadOnlyCollection<string> roles,
        Action<QylAdminFilterOptions> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<TestAuthOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                options => options.Roles = roles);

        builder.Services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<AdminFilterTestTools>()
            .WithQylAdminFilter(configure);

        var app = builder.Build();
        app.UseAuthentication();
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

        return new AdminFilterFixture(app, httpClient, transport, client);
    }

    public static string GetText(ModelContextProtocol.Protocol.CallToolResult result)
    {
        var block = result.Content
            .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .FirstOrDefault();
        return block?.Text ?? string.Empty;
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
