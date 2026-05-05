using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.DevUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ANcpLua.Agents.Hosting.DevUI;

public static class QylDevUIExtensions
{
    public static IHostApplicationBuilder AddQylDevUI(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);

        return MicrosoftAgentAIDevUIHostApplicationBuilderExtensions.AddDevUI(builder);
    }

    public static IServiceCollection AddQylDevUI(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return MicrosoftAgentAIDevUIServiceCollectionsExtensions.AddDevUI(services);
    }

    public static IEndpointConventionBuilder MapQylDevUI(this IEndpointRouteBuilder endpoints)
    {
        Guard.NotNull(endpoints);

        return DevUIExtensions.MapDevUI(endpoints);
    }

}
