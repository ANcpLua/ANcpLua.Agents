using ANcpLua.Roslyn.Utilities;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Hosting.Foundry;

public static class QylFoundryHostingExtensions
{
    public static IServiceCollection AddQylFoundryResponses(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return FoundryHostingExtensions.AddFoundryResponses(services);
    }

    public static IServiceCollection AddQylFoundryResponses(
        this IServiceCollection services,
        AIAgent agent,
        AgentSessionStore? agentSessionStore = null)
    {
        Guard.NotNull(services);
        Guard.NotNull(agent);

        return FoundryHostingExtensions.AddFoundryResponses(services, agent, agentSessionStore);
    }

    public static IServiceCollection AddQylFoundryToolboxes(
        this IServiceCollection services,
        params string[] toolboxNames)
    {
        Guard.NotNull(services);
        Guard.NotNull(toolboxNames);

        return FoundryHostingExtensions.AddFoundryToolboxes(services, toolboxNames);
    }

    public static IServiceCollection AddQylFoundryToolboxes(
        this IServiceCollection services,
        Action<FoundryToolboxOptions> configureOptions,
        params string[] toolboxNames)
    {
        Guard.NotNull(services);
        Guard.NotNull(configureOptions);
        Guard.NotNull(toolboxNames);

        return FoundryHostingExtensions.AddFoundryToolboxes(services, configureOptions, toolboxNames);
    }

    public static IEndpointRouteBuilder MapQylFoundryResponses(
        this IEndpointRouteBuilder endpoints,
        string prefix = "")
    {
        Guard.NotNull(endpoints);

        return FoundryHostingExtensions.MapFoundryResponses(endpoints, prefix);
    }

    public static Task<IReadOnlyList<AITool>> GetQylToolboxToolsAsync(
        this AIProjectClient client,
        string name,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(name);

        return client.GetToolboxToolsAsync(name, version, cancellationToken);
    }
}
