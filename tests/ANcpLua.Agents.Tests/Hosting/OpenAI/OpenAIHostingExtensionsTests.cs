using System.Reflection;
using System.Linq;
using ANcpLua.Agents.Hosting.OpenAI;
using ANcpLua.Agents.Testing.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace ANcpLua.Agents.Tests.Hosting.OpenAI;

public sealed class OpenAIHostingExtensionsTests
{
    [Fact]
    public void AddQylOpenAISurfaces_WithBuilder_ReturnsBuilderAndRegistersServices()
    {
        var builder = WebApplication.CreateBuilder();
        var before = builder.Services.Count;
        var result = builder.AddQylOpenAISurfaces();

        result.Should().Be(builder);
        builder.Services.Count.Should().BeGreaterThan(before);
    }

    [Fact]
    public void AddQylOpenAISurfaces_WithServices_ReturnsServicesAndRegistersServices()
    {
        var services = new ServiceCollection();
        var before = services.Count;
        var result = services.AddQylOpenAISurfaces();

        result.Should().BeSameAs(services);
        services.Count.Should().BeGreaterThan(before);
    }

    [Fact]
    public void MapQylOpenAISurfaces_WithAgent_ReturnsEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddQylOpenAISurfaces();
        using var app = builder.Build();
        var agent = new FakeDelegatingAgent { NameFunc = static () => "test-agent" };

        var result = app.MapQylOpenAISurfaces(agent);

        result.Should().BeSameAs((IEndpointRouteBuilder)app);
    }

    [Fact]
    public void MapQylOpenAISurfaces_WithHostedAgentBuilder_IsDefinedWithExpectedShape()
    {
        var method = typeof(QylOpenAIHostingExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(static m =>
                m.Name == nameof(QylOpenAIHostingExtensions.MapQylOpenAISurfaces) &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[1].ParameterType == typeof(IHostedAgentBuilder));

        method.ReturnType.Should().Be(typeof(IEndpointRouteBuilder));
        method.GetParameters()[0].ParameterType.Should().Be(typeof(IEndpointRouteBuilder));
    }
}
