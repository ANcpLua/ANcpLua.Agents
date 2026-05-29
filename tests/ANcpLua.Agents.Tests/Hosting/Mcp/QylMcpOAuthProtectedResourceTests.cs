using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ANcpLua.Agents.Mcp.Hosting.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.AspNetCore.Authentication;

namespace ANcpLua.Agents.Tests.Hosting.Mcp;

public sealed class QylMcpOAuthProtectedResourceTests
{
    [Fact]
    public async Task WithQylOAuthProtectedResource_RegistersJwtAndMcpSchemes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithQylOAuthProtectedResource(o =>
            {
                o.Authority = "https://idp.example.com/realms/qyl";
                o.Audience = "qyl-mcp";
                o.ResolveResourceUrl = req => new Uri($"{req.Scheme}://{req.Host}/mcp");
            });

        await using var app = builder.Build();
        await app.StartAsync();

        var schemes = app.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var registered = await schemes.GetAllSchemesAsync();
        var names = registered.Select(static s => s.Name).ToArray();

        names.Should().Contain(JwtBearerDefaults.AuthenticationScheme);
        names.Should().Contain(McpAuthenticationDefaults.AuthenticationScheme);

        var jwt = await schemes.GetSchemeAsync(JwtBearerDefaults.AuthenticationScheme);
        jwt.Should().NotBeNull();
        var jwtOptions = app.Services
            .GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        jwtOptions.Authority.Should().Be("https://idp.example.com/realms/qyl");
        jwtOptions.Audience.Should().Be("qyl-mcp");
        jwtOptions.RequireHttpsMetadata.Should().BeTrue();
        jwtOptions.MapInboundClaims.Should().BeFalse();
        jwtOptions.TokenValidationParameters.ValidateAudience.Should().BeTrue();

        await app.StopAsync();
    }

    [Fact]
    public async Task WithQylOAuthProtectedResource_OnResourceMetadataRequest_BuildsMetadataFromCallback()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithQylOAuthProtectedResource(o =>
            {
                o.Authority = "https://idp.example.com/realms/qyl";
                o.Audience = "qyl-mcp";
                o.ResolveResourceUrl = req => new Uri($"{req.Scheme}://{req.Host}/mcp");
                o.ConfigureMetadata = m =>
                {
                    m.ResourceName = "qyl-test";
                    m.ResourceDocumentation = new Uri("https://example.com/docs");
                };
            });

        await using var app = builder.Build();
        app.UseAuthentication();
        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.GetAsync("/.well-known/oauth-protected-resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();

        doc.GetProperty("resource").GetString().Should().Be("http://localhost/mcp");
        doc.GetProperty("authorization_servers").EnumerateArray()
            .Select(static e => e.GetString())
            .Should().ContainSingle().Which.Should().Be("https://idp.example.com/realms/qyl");
        doc.GetProperty("bearer_methods_supported").EnumerateArray()
            .Select(static e => e.GetString())
            .Should().ContainSingle().Which.Should().Be("header");
        doc.GetProperty("resource_name").GetString().Should().Be("qyl-test");
        doc.GetProperty("resource_documentation").GetString().Should().Be("https://example.com/docs");

        await app.StopAsync();
    }

    [Fact]
    public async Task WithQylOAuthProtectedResource_ConfigureJwtEvents_CallbackFires()
    {
        var failures = 0;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithQylOAuthProtectedResource(o =>
            {
                o.Authority = "https://idp.example.com/realms/qyl";
                o.Audience = "qyl-mcp";
                o.ResolveResourceUrl = req => new Uri($"{req.Scheme}://{req.Host}/mcp");
                o.ConfigureJwtEvents = events =>
                {
                    events.OnAuthenticationFailed = _ =>
                    {
                        Interlocked.Increment(ref failures);
                        return Task.CompletedTask;
                    };
                };
            });
        builder.Services.AddAuthorization();

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/protected", () => "ok").RequireAuthorization(policy =>
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
        await app.StartAsync();

        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", "not-a-real-jwt");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        failures.Should().BeGreaterThan(0, "the consumer-supplied OnAuthenticationFailed must fire when JWT validation rejects an inbound token");

        await app.StopAsync();
    }
}
