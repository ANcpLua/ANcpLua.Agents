using ANcpLua.Agents.Workflows.Execution;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Workflows;

/// <summary>
/// Qyl-prefixed helpers for building workflows from MAF agents.
/// </summary>
public static class QylAgentWorkflowExtensions
{
    /// <summary>
    /// Builds a sequential workflow from the supplied agents. When
    /// <paramref name="chainOnlyAgentResponses"/> is <see langword="true"/>, each agent
    /// receives only the previous agent's own output instead of the accumulated
    /// conversation (MAF 1.13 <c>SequentialWorkflowBuilder.WithChainOnlyAgentResponses</c>).
    /// </summary>
    /// <param name="agents">The agents to run in order.</param>
    /// <param name="chainOnlyAgentResponses">Whether to pass only each agent's own response downstream.</param>
    /// <returns>The sequential workflow.</returns>
    public static Workflow BuildQylSequential(this IEnumerable<AIAgent> agents, bool chainOnlyAgentResponses = false)
    {
        Guard.NotNull(agents);
        return AgentWorkflowBuilder.BuildSequential(chainOnlyAgentResponses, agents);
    }

    /// <summary>
    /// Builds a named sequential workflow from the supplied agents.
    /// </summary>
    /// <param name="agents">The agents to run in order.</param>
    /// <param name="workflowName">The workflow name.</param>
    /// <param name="chainOnlyAgentResponses">Whether to pass only each agent's own response downstream.</param>
    /// <returns>The sequential workflow.</returns>
    public static Workflow BuildQylSequential(
        this IEnumerable<AIAgent> agents,
        string workflowName,
        bool chainOnlyAgentResponses = false)
    {
        Guard.NotNull(agents);
        Guard.NotNullOrWhiteSpace(workflowName);
        return AgentWorkflowBuilder.BuildSequential(workflowName, chainOnlyAgentResponses, agents);
    }

    /// <summary>
    /// Builds a concurrent workflow from the supplied agents.
    /// </summary>
    /// <param name="agents">The agents to run concurrently.</param>
    /// <returns>The concurrent workflow.</returns>
    public static Workflow BuildQylConcurrent(this IEnumerable<AIAgent> agents)
    {
        Guard.NotNull(agents);
        return AgentWorkflowBuilder.BuildConcurrent(agents);
    }

    /// <summary>
    /// Creates a group-chat workflow builder and adds the supplied agents as participants.
    /// </summary>
    /// <param name="agents">The group-chat participants.</param>
    /// <param name="managerFactory">Factory that creates the group-chat manager from the participants.</param>
    /// <returns>The group-chat workflow builder.</returns>
    public static GroupChatWorkflowBuilder BuildQylGroupChat(
        this IEnumerable<AIAgent> agents,
        Func<IReadOnlyList<AIAgent>, GroupChatManager> managerFactory)
    {
        Guard.NotNull(agents);
        Guard.NotNull(managerFactory);
        return AgentWorkflowBuilder.CreateGroupChatBuilderWith(managerFactory).AddParticipants(agents);
    }

    /// <summary>
    /// Builds a handoff workflow rooted at <paramref name="rootAgent"/>. The
    /// <paramref name="configure"/> callback receives the underlying
    /// <see cref="HandoffWorkflowBuilder"/> to register
    /// <c>WithHandoffs(source, [targets])</c> / <c>WithHandoff(source, target, reason)</c> /
    /// <c>WithHandoffInstructions</c> / <c>EnableReturnToPrevious</c>.
    /// Completes the 3-of-3 first-class orchestration kinds (Sequential ✓, Concurrent ✓, Handoff ✓).
    /// </summary>
    public static Workflow BuildQylHandoff(
        this AIAgent rootAgent,
        Action<HandoffWorkflowBuilder> configure)
    {
        Guard.NotNull(rootAgent);
        Guard.NotNull(configure);

        var builder = AgentWorkflowBuilder.CreateHandoffBuilderWith(rootAgent);
        configure(builder);
        return builder.Build();
    }

    /// <summary>
    /// Wraps a handoff workflow as an <see cref="AIAgent"/> with the supplied
    /// <paramref name="name"/>, ready to compose into outer pipelines or expose as a tool.
    /// </summary>
    public static AIAgent AsQylHandoffAgent(
        this AIAgent rootAgent,
        string name,
        Action<HandoffWorkflowBuilder> configure)
    {
        Guard.NotNullOrWhiteSpace(name);
        return rootAgent.BuildQylHandoff(configure).AsAIAgent(name: name);
    }

    /// <summary>
    /// Wraps a sequential workflow as an agent.
    /// </summary>
    /// <param name="agents">The agents to run in order.</param>
    /// <param name="name">The resulting workflow-agent name.</param>
    /// <param name="chainOnlyAgentResponses">Whether to pass only each agent's own response downstream.</param>
    /// <returns>The sequential workflow exposed as an agent.</returns>
    public static AIAgent AsQylSequentialAgent(
        this IEnumerable<AIAgent> agents,
        string name,
        bool chainOnlyAgentResponses = false)
    {
        Guard.NotNull(agents);
        Guard.NotNullOrWhiteSpace(name);
        return AgentWorkflowBuilder.BuildSequential(chainOnlyAgentResponses, agents).AsAIAgent(name: name);
    }

    /// <summary>
    /// Wraps a concurrent workflow as an agent.
    /// </summary>
    /// <param name="agents">The agents to run concurrently.</param>
    /// <param name="name">The resulting workflow-agent name.</param>
    /// <returns>The concurrent workflow exposed as an agent.</returns>
    public static AIAgent AsQylConcurrentAgent(this IEnumerable<AIAgent> agents, string name)
    {
        Guard.NotNull(agents);
        Guard.NotNullOrWhiteSpace(name);
        return AgentWorkflowBuilder.BuildConcurrent(agents).AsAIAgent(name: name);
    }

    /// <summary>
    /// Streams a sequential agent workflow for a chat-message input.
    /// </summary>
    /// <param name="agents">The agents to run in order.</param>
    /// <param name="input">The chat-message input.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="emitEvents">Whether to emit workflow events.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
    public static ValueTask<StreamingRun> StreamQylSequentialAsync(
        this IEnumerable<AIAgent> agents,
        ChatMessage input,
        string? sessionId = null,
        bool emitEvents = true,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agents);
        Guard.NotNull(input);
        return QylWorkflowExecutionHelpers.StreamQylSequentialAsync(agents, input, sessionId, emitEvents, cancellationToken);
    }

    /// <summary>
    /// Streams a sequential agent workflow for a user prompt.
    /// </summary>
    /// <param name="agents">The agents to run in order.</param>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="emitEvents">Whether to emit workflow events.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
    public static ValueTask<StreamingRun> StreamQylSequentialAsync(
        this IEnumerable<AIAgent> agents,
        string prompt,
        string? sessionId = null,
        bool emitEvents = true,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agents);
        Guard.NotNullOrWhiteSpace(prompt);
        return QylWorkflowExecutionHelpers.StreamQylSequentialAsync(
            agents,
            new ChatMessage(ChatRole.User, prompt),
            sessionId,
            emitEvents,
            cancellationToken);
    }

    /// <summary>
    /// Streams a concurrent agent workflow for a chat-message input.
    /// </summary>
    /// <param name="agents">The agents to run concurrently.</param>
    /// <param name="input">The chat-message input.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="emitEvents">Whether to emit workflow events.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
    public static ValueTask<StreamingRun> StreamQylConcurrentAsync(
        this IEnumerable<AIAgent> agents,
        ChatMessage input,
        string? sessionId = null,
        bool emitEvents = true,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agents);
        Guard.NotNull(input);
        return QylWorkflowExecutionHelpers.StreamQylConcurrentAsync(agents, input, sessionId, emitEvents, cancellationToken);
    }

    /// <summary>
    /// Streams a concurrent agent workflow for a user prompt.
    /// </summary>
    /// <param name="agents">The agents to run concurrently.</param>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="emitEvents">Whether to emit workflow events.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
    public static ValueTask<StreamingRun> StreamQylConcurrentAsync(
        this IEnumerable<AIAgent> agents,
        string prompt,
        string? sessionId = null,
        bool emitEvents = true,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agents);
        Guard.NotNullOrWhiteSpace(prompt);
        return QylWorkflowExecutionHelpers.StreamQylConcurrentAsync(
            agents,
            new ChatMessage(ChatRole.User, prompt),
            sessionId,
            emitEvents,
            cancellationToken);
    }
}
