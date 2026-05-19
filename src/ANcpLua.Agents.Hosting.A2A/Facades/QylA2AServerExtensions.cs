using A2A;
using A2A.AspNetCore;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.A2A;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ANcpLua.Agents.Hosting.A2A;

/// <summary>
/// Qyl-prefixed facades over MAF Agent2Agent (A2A) server hosting APIs.
/// </summary>
public static class QylA2AServerExtensions
{
    /// <summary>
    /// Registers an A2A server for the specified agent on the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="agent">The agent to expose over A2A. The agent's <see cref="AIAgent.Name"/> is used as the registration key.</param>
    /// <param name="configureOptions">Optional callback to configure <see cref="A2AServerRegistrationOptions"/>.</param>
    /// <returns>The same host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddQylA2AServer(
        this IHostApplicationBuilder builder,
        AIAgent agent,
        Action<A2AServerRegistrationOptions>? configureOptions = null)
    {
        Guard.NotNull(builder);
        Guard.NotNull(agent);

        return A2AServerServiceCollectionExtensions.AddA2AServer(builder, agent, configureOptions);
    }

    /// <summary>
    /// Registers an A2A server keyed by the specified agent name on the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="agentName">The agent name used as the registration key.</param>
    /// <param name="configureOptions">Optional callback to configure <see cref="A2AServerRegistrationOptions"/>.</param>
    /// <returns>The same host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddQylA2AServer(
        this IHostApplicationBuilder builder,
        string agentName,
        Action<A2AServerRegistrationOptions>? configureOptions = null)
    {
        Guard.NotNull(builder);
        Guard.NotNullOrWhiteSpace(agentName);

        return A2AServerServiceCollectionExtensions.AddA2AServer(builder, agentName, configureOptions);
    }

    /// <summary>
    /// Registers an A2A server for the specified agent on the service collection.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="agent">The agent to expose over A2A. The agent's <see cref="AIAgent.Name"/> is used as the registration key.</param>
    /// <param name="configureOptions">Optional callback to configure <see cref="A2AServerRegistrationOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylA2AServer(
        this IServiceCollection services,
        AIAgent agent,
        Action<A2AServerRegistrationOptions>? configureOptions = null)
    {
        Guard.NotNull(services);
        Guard.NotNull(agent);

        return A2AServerServiceCollectionExtensions.AddA2AServer(services, agent, configureOptions);
    }

    /// <summary>
    /// Registers an A2A server keyed by the specified agent name on the service collection.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="agentName">The agent name used as the registration key.</param>
    /// <param name="configureOptions">Optional callback to configure <see cref="A2AServerRegistrationOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylA2AServer(
        this IServiceCollection services,
        string agentName,
        Action<A2AServerRegistrationOptions>? configureOptions = null)
    {
        Guard.NotNull(services);
        Guard.NotNullOrWhiteSpace(agentName);

        return A2AServerServiceCollectionExtensions.AddA2AServer(services, agentName, configureOptions);
    }

    /// <summary>
    /// Maps A2A JSON-RPC endpoints AND the well-known agent-card endpoint for the specified agent in one call.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agent">The agent whose registered A2A server should be exposed.</param>
    /// <param name="agentCard">The agent card published at the well-known discovery URL.</param>
    /// <param name="path">The route path prefix for the JSON-RPC endpoint. Defaults to <c>"/"</c>.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapQylA2A(
        this IEndpointRouteBuilder endpoints,
        AIAgent agent,
        AgentCard agentCard,
        string path = "/")
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agent);
        Guard.NotNull(agentCard);
        Guard.NotNullOrWhiteSpace(path);

        A2AEndpointRouteBuilderExtensions.MapA2AJsonRpc(endpoints, agent, path);
        A2ARouteBuilderExtensions.MapWellKnownAgentCard(endpoints, agentCard);

        return endpoints;
    }

    /// <summary>
    /// Maps A2A JSON-RPC endpoints for the specified agent at the given path.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agent">The agent whose registered A2A server should be exposed.</param>
    /// <param name="path">The route path prefix for the JSON-RPC endpoint.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylA2AJsonRpc(
        this IEndpointRouteBuilder endpoints,
        AIAgent agent,
        string path)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agent);
        Guard.NotNullOrWhiteSpace(path);

        return A2AEndpointRouteBuilderExtensions.MapA2AJsonRpc(endpoints, agent, path);
    }

    /// <summary>
    /// Maps A2A HTTP+JSON endpoints for the specified agent at the given path.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agent">The agent whose registered A2A server should be exposed.</param>
    /// <param name="path">The route path prefix for the HTTP+JSON endpoint.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylA2AHttpJson(
        this IEndpointRouteBuilder endpoints,
        AIAgent agent,
        string path)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agent);
        Guard.NotNullOrWhiteSpace(path);

        return A2AEndpointRouteBuilderExtensions.MapA2AHttpJson(endpoints, agent, path);
    }

    /// <summary>
    /// Maps the well-known agent-card discovery endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agentCard">The agent card to publish.</param>
    /// <param name="path">The route path prefix. Defaults to the A2A well-known location.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylWellKnownAgentCard(
        this IEndpointRouteBuilder endpoints,
        AgentCard agentCard,
        string path = "")
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agentCard);

        return A2ARouteBuilderExtensions.MapWellKnownAgentCard(endpoints, agentCard, path);
    }
}
