using ANcpLua.Roslyn.Utilities;
using Anthropic;
using Anthropic.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Agents.Hosting.Anthropic;

/// <summary>
/// Qyl-prefixed facades over MAF Anthropic agent APIs.
/// </summary>
public static class QylAnthropicAgentExtensions
{
    /// <summary>
    /// Creates a <see cref="ChatClientAgent"/> from an Anthropic client and model.
    /// </summary>
    /// <param name="client">The Anthropic client.</param>
    /// <param name="model">The Anthropic model id.</param>
    /// <param name="instructions">Optional system instructions.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
    /// <param name="tools">Optional agent tools.</param>
    /// <param name="defaultMaxTokens">Optional default maximum output token count.</param>
    /// <param name="clientFactory">Optional chat-client decorator factory.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="services">Optional service provider.</param>
    /// <returns>The configured Anthropic-backed agent.</returns>
    public static ChatClientAgent AsQylAnthropicAgent(
        this IAnthropicClient client,
        string model,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        int? defaultMaxTokens = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(model);

        return client.AsQylAIAgent(
            model,
            instructions,
            name,
            description,
            tools,
            defaultMaxTokens,
            clientFactory,
            loggerFactory,
            services);
    }

    /// <summary>
    /// Creates a <see cref="ChatClientAgent"/> from an Anthropic client, model, and hosted MCP servers.
    /// </summary>
    /// <param name="client">The Anthropic client.</param>
    /// <param name="model">The Anthropic model id.</param>
    /// <param name="mcpServers">Hosted MCP server tools to expose to the agent.</param>
    /// <param name="instructions">Optional system instructions.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
    /// <param name="tools">Optional additional agent tools.</param>
    /// <param name="defaultMaxTokens">Optional default maximum output token count.</param>
    /// <param name="clientFactory">Optional chat-client decorator factory.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="services">Optional service provider.</param>
    /// <returns>The configured Anthropic-backed agent.</returns>
    public static ChatClientAgent AsQylAnthropicAgent(
        this IAnthropicClient client,
        string model,
        IList<HostedMcpServerTool> mcpServers,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        int? defaultMaxTokens = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(model);
        Guard.NotNull(mcpServers);

        IList<AITool> allTools = tools is { Count: > 0 }
            ? [.. tools, .. mcpServers]
            : [.. mcpServers];

        return client.AsQylAIAgent(
            model,
            instructions,
            name,
            description,
            allTools,
            defaultMaxTokens,
            clientFactory,
            loggerFactory,
            services);
    }

    /// <summary>
    /// Delegates to Anthropic's native <c>AsAIAgent</c> extension while preserving Qyl naming.
    /// </summary>
    /// <param name="client">The Anthropic client.</param>
    /// <param name="model">The Anthropic model id.</param>
    /// <param name="instructions">Optional system instructions.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
    /// <param name="tools">Optional agent tools.</param>
    /// <param name="defaultMaxTokens">Optional default maximum output token count.</param>
    /// <param name="clientFactory">Optional chat-client decorator factory.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="services">Optional service provider.</param>
    /// <returns>The configured Anthropic-backed agent.</returns>
    public static ChatClientAgent AsQylAIAgent(
        this IAnthropicClient client,
        string model,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        int? defaultMaxTokens = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(model);

        return global::Anthropic.AnthropicClientExtensions.AsAIAgent(
            client,
            model,
            instructions,
            name,
            description,
            tools,
            defaultMaxTokens,
            clientFactory,
            loggerFactory,
            services);
    }

    /// <summary>
    /// Delegates to Anthropic's native <c>AsAIAgent</c> options overload while preserving Qyl naming.
    /// </summary>
    /// <param name="client">The Anthropic client.</param>
    /// <param name="options">The agent construction options.</param>
    /// <param name="clientFactory">Optional chat-client decorator factory.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="services">Optional service provider.</param>
    /// <returns>The configured Anthropic-backed agent.</returns>
    public static ChatClientAgent AsQylAIAgent(
        this IAnthropicClient client,
        ChatClientAgentOptions options,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);
        Guard.NotNull(options);

        return global::Anthropic.AnthropicClientExtensions.AsAIAgent(client, options, clientFactory, loggerFactory, services);
    }

    /// <summary>
    /// Delegates to Anthropic beta-service agent construction while preserving Qyl naming.
    /// </summary>
    /// <param name="betaService">The Anthropic beta service.</param>
    /// <param name="model">The Anthropic model id.</param>
    /// <param name="instructions">Optional system instructions.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
    /// <param name="tools">Optional agent tools.</param>
    /// <param name="defaultMaxTokens">Optional default maximum output token count.</param>
    /// <param name="clientFactory">Optional chat-client decorator factory.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="services">Optional service provider.</param>
    /// <returns>The configured Anthropic beta-backed agent.</returns>
    public static ChatClientAgent AsQylAIAgent(
        this IBetaService betaService,
        string model,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        int? defaultMaxTokens = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(betaService);
        Guard.NotNullOrWhiteSpace(model);

        return AnthropicBetaServiceExtensions.AsAIAgent(
            betaService,
            model,
            instructions,
            name,
            description,
            tools,
            defaultMaxTokens,
            clientFactory,
            loggerFactory,
            services);
    }

    /// <summary>
    /// Delegates to Anthropic beta-service options-based agent construction while preserving Qyl naming.
    /// </summary>
    /// <param name="betaService">The Anthropic beta service.</param>
    /// <param name="options">The agent construction options.</param>
    /// <param name="clientFactory">Optional chat-client decorator factory.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="services">Optional service provider.</param>
    /// <returns>The configured Anthropic beta-backed agent.</returns>
    public static ChatClientAgent AsQylAIAgent(
        this IBetaService betaService,
        ChatClientAgentOptions options,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(betaService);
        Guard.NotNull(options);

        return AnthropicBetaServiceExtensions.AsAIAgent(betaService, options, clientFactory, loggerFactory, services);
    }
}
