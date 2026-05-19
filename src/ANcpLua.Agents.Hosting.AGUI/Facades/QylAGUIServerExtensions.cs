using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Hosting.AGUI;

/// <summary>
/// Qyl-prefixed facades over MAF AG-UI (CopilotKit) hosting APIs.
/// </summary>
public static class QylAGUIServerExtensions
{
    /// <summary>
    /// Adds AG-UI services to the DI service collection.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylAGUI(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return MicrosoftAgentAIHostingAGUIServiceCollectionExtensions.AddAGUI(services);
    }

    /// <summary>
    /// Maps an AG-UI streaming endpoint for the given <paramref name="agent"/> at the given <paramref name="pattern"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (for example, <c>"/"</c> or <c>"/weather"</c>).</param>
    /// <param name="agent">The agent to expose over AG-UI.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylAGUI(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        AIAgent agent)
    {
        Guard.NotNull(endpoints);
        Guard.NotNullOrWhiteSpace(pattern);
        Guard.NotNull(agent);

        return AGUIEndpointRouteBuilderExtensions.MapAGUI(endpoints, pattern, agent);
    }
}
