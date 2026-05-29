using A2A;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.A2A;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Agents.Hosting.A2A;

/// <summary>
/// Qyl-prefixed facades for connecting to remote Agent2Agent (A2A) endpoints as <see cref="AIAgent"/> instances.
/// </summary>
public static class QylA2AClientExtensions
{
    /// <summary>
    /// Resolves the A2A agent card published at the well-known URI under the supplied base address
    /// and returns an <see cref="AIAgent"/> wired to call it.
    /// </summary>
    /// <param name="uri">The base URI of the remote A2A endpoint.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> reused for both card resolution and agent traffic.</param>
    /// <param name="options">Optional client options controlling protocol-binding preference (HTTP+JSON vs JSON-RPC).</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the agent-card resolution to complete.</param>
    /// <returns>An <see cref="AIAgent"/> backed by the remote A2A endpoint.</returns>
    public static Task<AIAgent> ConnectQylA2AAsync(
        Uri uri,
        HttpClient? httpClient = null,
        A2AClientOptions? options = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(uri);

        A2ACardResolver resolver = new(uri);
        return resolver.GetAIAgentAsync(httpClient, options, loggerFactory, cancellationToken);
    }

    /// <summary>
    /// Returns an <see cref="AIAgent"/> backed by the remote A2A endpoint described by the supplied
    /// <see cref="A2ACardResolver"/>.
    /// </summary>
    /// <param name="resolver">A pre-configured <see cref="A2ACardResolver"/>.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> reused for both card resolution and agent traffic.</param>
    /// <param name="options">Optional client options controlling protocol-binding preference (HTTP+JSON vs JSON-RPC).</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the agent-card resolution to complete.</param>
    /// <returns>An <see cref="AIAgent"/> backed by the remote A2A endpoint.</returns>
    public static Task<AIAgent> ConnectQylA2AAsync(
        this A2ACardResolver resolver,
        HttpClient? httpClient = null,
        A2AClientOptions? options = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(resolver);

        return resolver.GetAIAgentAsync(httpClient, options, loggerFactory, cancellationToken);
    }

    /// <summary>
    /// Returns an <see cref="AIAgent"/> backed by the supplied <see cref="AgentCard"/>.
    /// </summary>
    /// <param name="card">The agent card describing the remote A2A endpoint.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> reused for agent traffic.</param>
    /// <param name="options">Optional client options controlling protocol-binding preference (HTTP+JSON vs JSON-RPC).</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <returns>An <see cref="AIAgent"/> backed by the remote A2A endpoint.</returns>
    public static AIAgent AsQylA2AAgent(
        this AgentCard card,
        HttpClient? httpClient = null,
        A2AClientOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        Guard.NotNull(card);

        return card.AsAIAgent(httpClient, options, loggerFactory);
    }

    /// <summary>
    /// Returns an <see cref="AIAgent"/> backed by the supplied A2A client, using the
    /// <see href="https://github.com/a2aproject/A2A/blob/main/docs/topics/agent-discovery.md#3-direct-configuration--private-discovery">direct configuration / private discovery</see>
    /// mechanism for endpoints known ahead of time.
    /// </summary>
    /// <param name="client">The A2A client pointing at the known remote endpoint.</param>
    /// <param name="id">Optional unique identifier for the agent.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <returns>An <see cref="AIAgent"/> backed by the remote A2A endpoint.</returns>
    public static AIAgent AsQylA2AAgent(
        this IA2AClient client,
        string? id = null,
        string? name = null,
        string? description = null,
        ILoggerFactory? loggerFactory = null)
    {
        Guard.NotNull(client);

        return client.AsAIAgent(id, name, description, loggerFactory);
    }

    /// <summary>
    /// Returns an <see cref="AIAgent"/> backed by the supplied A2A client, taking agent identity
    /// (id, name, description) from the supplied <see cref="A2AAgentOptions"/>.
    /// </summary>
    /// <param name="client">The A2A client pointing at the known remote endpoint.</param>
    /// <param name="options">Options controlling the agent's identity.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <returns>An <see cref="AIAgent"/> backed by the remote A2A endpoint.</returns>
    public static AIAgent AsQylA2AAgent(
        this IA2AClient client,
        A2AAgentOptions options,
        ILoggerFactory? loggerFactory = null)
    {
        Guard.NotNull(client);
        Guard.NotNull(options);

        return client.AsAIAgent(options, loggerFactory);
    }
}
