using ANcpLua.Agents.Mcp.Hosting.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ANcpLua.Agents.Tests.Hosting.Mcp;

public sealed class QylMcpWebHostTests
{
    [Fact]
    public void UseQylMcpPortFallback_NoExistingUrls_PortEnvSet_BindsToPort()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PORT"] = "5847"
            })
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseQylMcpPortFallback(configuration);

        builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey)
            .Should().Be("http://0.0.0.0:5847");
    }

    [Fact]
    public void UseQylMcpPortFallback_ExistingAspNetCoreUrls_DoesNothing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PORT"] = "5847",
                ["ASPNETCORE_URLS"] = "http://localhost:7777"
            })
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://localhost:7777");
        builder.WebHost.UseQylMcpPortFallback(configuration);

        builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey)
            .Should().Be("http://localhost:7777");
    }

    [Fact]
    public void UseQylMcpPortFallback_ExistingDotnetUrls_DoesNothing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PORT"] = "5847",
                ["DOTNET_URLS"] = "http://localhost:7777"
            })
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseQylMcpPortFallback(configuration);

        builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey)
            .Should().BeNullOrEmpty();
    }

    [Fact]
    public void UseQylMcpPortFallback_NoPortAndNoUrls_DoesNothing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseQylMcpPortFallback(configuration);

        builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey)
            .Should().BeNullOrEmpty();
    }

    [Fact]
    public void UseQylMcpPortFallback_NonNumericPort_DoesNothing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PORT"] = "not-a-number"
            })
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseQylMcpPortFallback(configuration);

        builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey)
            .Should().BeNullOrEmpty();
    }
}
