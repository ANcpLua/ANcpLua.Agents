using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.DevUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ANcpLua.Agents.Hosting.DevUI;

/// <summary>
/// Qyl-prefixed facades over MAF DevUI hosting APIs.
/// </summary>
public static class QylDevUIExtensions
{
    /// <summary>
    /// Adds DevUI services to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddQylDevUI(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);

        return MicrosoftAgentAIDevUIHostApplicationBuilderExtensions.AddDevUI(builder);
    }

    /// <summary>
    /// Adds DevUI services to the service collection.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylDevUI(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return MicrosoftAgentAIDevUIServiceCollectionsExtensions.AddDevUI(services);
    }

    /// <summary>
    /// Maps DevUI endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylDevUI(this IEndpointRouteBuilder endpoints)
    {
        Guard.NotNull(endpoints);

        return DevUIExtensions.MapDevUI(endpoints);
    }
}
