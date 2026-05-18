using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ANcpLua.Agents.Hosting.Foundry;

/// <summary>
/// Qyl-prefixed facades over MAF Foundry hosted-agent APIs.
/// </summary>
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

        services.Replace(ServiceDescriptor.Singleton<AgentSessionStore>(agentSessionStore));
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

    /// <summary>
    /// Adds the named Foundry toolboxes to the hosted-agent service graph.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="toolboxNames">The toolbox names to register.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylFoundryToolboxes(
        this IServiceCollection services,
        params string[] toolboxNames)
    {
        Guard.NotNull(services);
        Guard.NotNull(toolboxNames);

        return FoundryHostingExtensions.AddFoundryToolboxes(services, toolboxNames);
    }

    /// <summary>
    /// Adds the named Foundry toolboxes to the hosted-agent service graph with caller-supplied options.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configureOptions">Configures toolbox retrieval options.</param>
    /// <param name="toolboxNames">The toolbox names to register.</param>
    /// <returns>The same service collection for chaining.</returns>
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

    /// <summary>
    /// Maps the Foundry Responses endpoint surface.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="prefix">Optional endpoint prefix.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapQylFoundryResponses(
        this IEndpointRouteBuilder endpoints,
        string prefix = "")
    {
        Guard.NotNull(endpoints);

        return FoundryHostingExtensions.MapFoundryResponses(endpoints, prefix);
    }
}
