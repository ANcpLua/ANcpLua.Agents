using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Hosting.OpenAI;

/// <summary>
///     Qyl-prefixed sugar for the OpenAI Responses API surface: hosted server-side tools
///     (CodeInterpreter / FileSearch / WebSearch), conversation resume, and the
///     background-response polling loop.
/// </summary>
public static class QylResponsesApiExtensions
{
    /// <summary>Appends a <see cref="HostedCodeInterpreterTool"/> to <paramref name="options"/>' tool list.</summary>
    public static ChatClientAgentRunOptions WithQylCodeInterpreter(this ChatClientAgentRunOptions options)
    {
        Guard.NotNull(options);
        EnsureTools(options).Add(new HostedCodeInterpreterTool());
        return options;
    }

    /// <summary>
    ///     Appends a <see cref="HostedFileSearchTool"/>, optionally pinning it to a vector-store id
    ///     or a set of hosted-file ids supplied as <see cref="HostedVectorStoreContent"/> /
    ///     <see cref="HostedFileContent"/> instances.
    /// </summary>
    public static ChatClientAgentRunOptions WithQylFileSearch(
        this ChatClientAgentRunOptions options,
        params AIContent[] inputs)
    {
        Guard.NotNull(options);
        Guard.NotNull(inputs);
        var tool = new HostedFileSearchTool();
        if (inputs.Length > 0)
            tool.Inputs = inputs;
        EnsureTools(options).Add(tool);
        return options;
    }

    /// <summary>Appends a <see cref="HostedWebSearchTool"/> to <paramref name="options"/>' tool list.</summary>
    public static ChatClientAgentRunOptions WithQylWebSearch(this ChatClientAgentRunOptions options)
    {
        Guard.NotNull(options);
        EnsureTools(options).Add(new HostedWebSearchTool());
        return options;
    }

    /// <summary>
    ///     Resumes a server-stored Responses conversation by <paramref name="conversationId"/>
    ///     (typically the previous response's <c>ResponseId</c>). Use after process restart.
    /// </summary>
    public static Task<AgentResponse> RunQylResumedAsync(
        this ChatClientAgent agent,
        string conversationId,
        string input,
        AgentSession? session = null,
        ChatClientAgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agent);
        Guard.NotNullOrWhiteSpace(conversationId);
        Guard.NotNullOrWhiteSpace(input);

        options ??= new ChatClientAgentRunOptions();
        options.ChatOptions ??= new ChatOptions();
        options.ChatOptions.ConversationId = conversationId;
        return agent.RunAsync(input, session, options, cancellationToken);
    }

    private static IList<AITool> EnsureTools(ChatClientAgentRunOptions options)
    {
        options.ChatOptions ??= new ChatOptions();
        options.ChatOptions.Tools ??= [];
        return options.ChatOptions.Tools;
    }
}
