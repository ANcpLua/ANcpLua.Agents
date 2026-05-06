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
    /// Builds a sequential workflow from the supplied agents.
    /// </summary>
    /// <param name="agents">The agents to run in order.</param>
    /// <returns>The sequential workflow.</returns>
    public static Workflow BuildQylSequential(this IEnumerable<AIAgent> agents)
    {
        Guard.NotNull(agents);
        return AgentWorkflowBuilder.BuildSequential(agents);
    }

    /// <summary>
    /// Builds a named sequential workflow from the supplied agents.
    /// </summary>
    /// <param name="agents">The agents to run in order.</param>
    /// <param name="workflowName">The workflow name.</param>
    /// <returns>The sequential workflow.</returns>
    public static Workflow BuildQylSequential(this IEnumerable<AIAgent> agents, string workflowName)
    {
        Guard.NotNull(agents);
        Guard.NotNullOrWhiteSpace(workflowName);
        return AgentWorkflowBuilder.BuildSequential(workflowName, agents);
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
    /// Wraps a sequential workflow as an agent.
    /// </summary>
    /// <param name="agents">The agents to run in order.</param>
    /// <param name="name">The resulting workflow-agent name.</param>
    /// <returns>The sequential workflow exposed as an agent.</returns>
    public static AIAgent AsQylSequentialAgent(this IEnumerable<AIAgent> agents, string name)
    {
        Guard.NotNull(agents);
        Guard.NotNullOrWhiteSpace(name);
        return AgentWorkflowBuilder.BuildSequential(agents).AsAIAgent(name: name);
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
