using ANcpLua.Roslyn.Utilities;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ANcpLua.Agents.Hosting.Foundry;

public static class QylFoundryHostingExtensions
{
    /// <summary>
    /// Wires the Foundry Responses pipeline with MAF's default <see cref="AgentSessionStore"/>.
    /// </summary>
    /// <remarks>
    /// As of MAF 1.4 the default is <c>FileSystemAgentSessionStore.CreateDefault()</c> (rooted at
    /// <c>/.checkpoints</c> in a Foundry-hosted environment, otherwise <c>{cwd}/.checkpoints</c>).
    /// MAF 1.3 defaulted to <c>InMemoryAgentSessionStore</c>; consumers that want in-memory
    /// behavior must opt in via the <c>(services, AgentSessionStore)</c> overload.
    /// </remarks>
    public static IServiceCollection AddQylFoundryResponses(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return FoundryHostingExtensions.AddFoundryResponses(services);
    }

    /// <summary>
    /// Wires the Foundry Responses pipeline with a caller-chosen <see cref="AgentSessionStore"/>.
    /// </summary>
    /// <remarks>
    /// Pre-registers <paramref name="agentSessionStore"/> so MAF's <c>TryAddSingleton</c> default
    /// (<c>FileSystemAgentSessionStore.CreateDefault()</c> in 1.4) is suppressed. Pass
    /// <c>new InMemoryAgentSessionStore()</c> to restore MAF 1.3 behavior, or any other
    /// <see cref="AgentSessionStore"/> implementation for custom persistence.
    /// </remarks>
    public static IServiceCollection AddQylFoundryResponses(
        this IServiceCollection services,
        AgentSessionStore agentSessionStore)
    {
        Guard.NotNull(services);
        Guard.NotNull(agentSessionStore);

        services.TryAddSingleton<AgentSessionStore>(agentSessionStore);
        return FoundryHostingExtensions.AddFoundryResponses(services);
    }

    /// <summary>
    /// Registers <paramref name="agent"/> as a hosted Foundry agent, with an optional
    /// caller-chosen <see cref="AgentSessionStore"/>.
    /// </summary>
    /// <remarks>
    /// When <paramref name="agentSessionStore"/> is <see langword="null"/>, MAF 1.4 falls back to
    /// <c>FileSystemAgentSessionStore.CreateDefault()</c> (was <c>InMemoryAgentSessionStore</c>
    /// in 1.3). Pass an explicit store to override.
    /// </remarks>
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
