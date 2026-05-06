// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using OpenAI.Responses;

namespace ANcpLua.Agents.Hosting.OpenAI;

/// <summary>
///     Qyl-prefixed OpenAI client-to-agent facades.
/// </summary>
public static class QylOpenAIClientExtensions
{
    /// <summary>
    ///     Adapts an OpenAI chat client to a <see cref="ChatClientAgent" />.
    /// </summary>
    public static ChatClientAgent AsQylOpenAIAgent(
        this ChatClient client,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);

        return OpenAIChatClientExtensions.AsAIAgent(
            client,
            instructions,
            name,
            description,
            tools,
            clientFactory,
            loggerFactory,
            services);
    }

    /// <summary>
    ///     Adapts an OpenAI chat client to a <see cref="ChatClientAgent" /> with prepared options.
    /// </summary>
    public static ChatClientAgent AsQylOpenAIAgent(
        this ChatClient client,
        ChatClientAgentOptions options,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);
        Guard.NotNull(options);

        return OpenAIChatClientExtensions.AsAIAgent(client, options, clientFactory, loggerFactory, services);
    }

    /// <summary>
    ///     Adapts an OpenAI responses client to a <see cref="ChatClientAgent" />.
    /// </summary>
    public static ChatClientAgent AsQylOpenAIAgent(
        this ResponsesClient client,
        string? model = null,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);

        return OpenAIResponseClientExtensions.AsAIAgent(
            client,
            model,
            instructions,
            name,
            description,
            tools,
            clientFactory,
            loggerFactory,
            services);
    }

    /// <summary>
    ///     Adapts an OpenAI responses client to a <see cref="ChatClientAgent" /> with prepared options.
    /// </summary>
    public static ChatClientAgent AsQylOpenAIAgent(
        this ResponsesClient client,
        ChatClientAgentOptions options,
        string? model = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);
        Guard.NotNull(options);

        return OpenAIResponseClientExtensions.AsAIAgent(client, options, model, clientFactory, loggerFactory, services);
    }

    /// <summary>
    ///     Adapts an OpenAI responses client to a <see cref="ChatClientAgent" /> with hosted MCP server tools.
    /// </summary>
    public static ChatClientAgent AsQylOpenAIAgent(
        this ResponsesClient client,
        IList<HostedMcpServerTool> mcpServers,
        string? model = null,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);
        Guard.NotNull(mcpServers);

        IList<AITool> allTools = tools is { Count: > 0 }
            ? [.. tools, .. mcpServers]
            : [.. mcpServers];

        return OpenAIResponseClientExtensions.AsAIAgent(
            client,
            model,
            instructions,
            name,
            description,
            allTools,
            clientFactory,
            loggerFactory,
            services);
    }

    /// <summary>
    ///     Returns an <see cref="IChatClient" /> over the given <see cref="ResponsesClient" /> with
    ///     <c>store</c> turned off (suitable for zero-data-retention organizations).
    /// </summary>
    public static IChatClient AsQylOpenAIChatClientWithStoredOutputDisabled(
        this ResponsesClient client,
        string? model = null,
        bool includeReasoningEncryptedContent = true)
    {
        Guard.NotNull(client);

        return client.AsIChatClientWithStoredOutputDisabled(model, includeReasoningEncryptedContent);
    }

    /// <summary>
    ///     Runs the agent against native OpenAI <c>OpenAI.Chat.ChatMessage</c> inputs and returns a
    ///     native <see cref="ChatCompletion" />.
    /// </summary>
    public static Task<ChatCompletion> RunQylOpenAIAsync(
        this AIAgent agent,
        IEnumerable<global::OpenAI.Chat.ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agent);
        Guard.NotNull(messages);

        return AIAgentWithOpenAIExtensions.RunAsync(agent, messages, session, options, cancellationToken);
    }

    /// <summary>
    ///     Runs the agent in streaming mode against native OpenAI <c>OpenAI.Chat.ChatMessage</c> inputs
    ///     and yields <see cref="StreamingChatCompletionUpdate" /> chunks.
    /// </summary>
    public static AsyncCollectionResult<StreamingChatCompletionUpdate> RunQylOpenAIStreamingAsync(
        this AIAgent agent,
        IEnumerable<global::OpenAI.Chat.ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agent);
        Guard.NotNull(messages);

        return AIAgentWithOpenAIExtensions.RunStreamingAsync(agent, messages, session, options, cancellationToken);
    }

    /// <summary>
    ///     Runs the agent against native OpenAI <see cref="ResponseItem" /> inputs and returns a
    ///     native <see cref="ResponseResult" />.
    /// </summary>
    public static Task<ResponseResult> RunQylOpenAIAsync(
        this AIAgent agent,
        IEnumerable<ResponseItem> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agent);
        Guard.NotNull(messages);

        return AIAgentWithOpenAIExtensions.RunAsync(agent, messages, session, options, cancellationToken);
    }

    /// <summary>
    ///     Runs the agent in streaming mode against native OpenAI <see cref="ResponseItem" />
    ///     inputs and yields <see cref="StreamingResponseUpdate" /> chunks.
    /// </summary>
    public static AsyncCollectionResult<StreamingResponseUpdate> RunQylOpenAIStreamingAsync(
        this AIAgent agent,
        IEnumerable<ResponseItem> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agent);
        Guard.NotNull(messages);

        return AIAgentWithOpenAIExtensions.RunStreamingAsync(agent, messages, session, options, cancellationToken);
    }

    /// <summary>
    ///     Extracts a native OpenAI <see cref="ChatCompletion" /> from an <see cref="AgentResponse" />.
    /// </summary>
    public static ChatCompletion AsQylOpenAIChatCompletion(this AgentResponse response)
    {
        Guard.NotNull(response);

        return response.AsOpenAIChatCompletion();
    }

    /// <summary>
    ///     Extracts a native OpenAI <see cref="ResponseResult" /> from an <see cref="AgentResponse" />.
    /// </summary>
    public static ResponseResult AsQylResponseResult(this AgentResponse response)
    {
        Guard.NotNull(response);

        return response.AsOpenAIResponse();
    }
}
