using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Instrumentation;

internal sealed class AgentToolTelemetryAgent : DelegatingAIAgent, IDisposable
{
    private readonly AgentTelemetryInstrumentation _telemetry;

    public AgentToolTelemetryAgent(AIAgent innerAgent, Action<AgentTelemetryOptions>? configure) : base(innerAgent)
    {
        if (innerAgent.GetService<FunctionInvokingChatClient>() is null)
        {
            throw new InvalidOperationException(
                $"The tool telemetry middleware can only decorate an {nameof(AIAgent)} that supports {nameof(FunctionInvokingChatClient)}.");
        }

        _telemetry = AgentTelemetryInstrumentation.Create(configure);
    }

    public void Dispose()
    {
        _telemetry.Dispose();
    }

    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return InnerAgent.RunAsync(messages, session, AgentRunOptionsWithToolTelemetry(options), cancellationToken);
    }

    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return InnerAgent.RunStreamingAsync(messages, session, AgentRunOptionsWithToolTelemetry(options), cancellationToken);
    }

    private AgentRunOptions AgentRunOptionsWithToolTelemetry(AgentRunOptions? options)
    {
        ChatClientAgentRunOptions chatClientOptions;

        if (options is ChatClientAgentRunOptions existingOptions)
        {
            chatClientOptions = (ChatClientAgentRunOptions)existingOptions.Clone();
        }
        else if (options is null || options.GetType() == typeof(AgentRunOptions))
        {
            chatClientOptions = new ChatClientAgentRunOptions();
            if (options is not null)
                CopyBaseAgentRunOptions(options, chatClientOptions);
        }
        else
        {
            throw new NotSupportedException(
                $"Tool telemetry middleware is only supported without options or with {nameof(ChatClientAgentRunOptions)}.");
        }

        var originalFactory = chatClientOptions.ChatClientFactory;
        chatClientOptions.ChatClientFactory = chatClient =>
        {
            var configuredChatClient = originalFactory is null ? chatClient : originalFactory(chatClient);
            return configuredChatClient.AsBuilder()
                .ConfigureOptions(chatOptions =>
                {
                    chatOptions.Tools = chatOptions.Tools?
                        .Select(tool => tool is AIFunction function
                            ? new TelemetryEnabledFunction(InnerAgent, function, _telemetry)
                            : tool)
                        .ToList();
                })
                .Build();
        };

        return chatClientOptions;
    }

    private static void CopyBaseAgentRunOptions(AgentRunOptions source, AgentRunOptions target)
    {
        target.AllowBackgroundResponses = source.AllowBackgroundResponses;
        target.AdditionalProperties = source.AdditionalProperties?.Clone();
        target.ResponseFormat = source.ResponseFormat;
    }

    private sealed class TelemetryEnabledFunction(
        AIAgent agent,
        AIFunction innerFunction,
        AgentTelemetryInstrumentation telemetry)
        : DelegatingAIFunction(innerFunction)
    {
        protected override ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var context = FunctionInvokingChatClient.CurrentContext
                ?? new FunctionInvocationContext
                {
                    Arguments = arguments,
                    Function = InnerFunction,
                    CallContent = new FunctionCallContent(string.Empty, InnerFunction.Name, new Dictionary<string, object?>(arguments)),
                };

            return telemetry.TrackToolAsync(
                agent,
                context.Function.Name,
                () => base.InvokeCoreAsync(context.Arguments, cancellationToken));
        }
    }
}
