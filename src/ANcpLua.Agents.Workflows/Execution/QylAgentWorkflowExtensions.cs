using ANcpLua.Agents.Workflows.Execution;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Workflows;

public static class QylAgentWorkflowExtensions
{
    public static Workflow BuildQylSequential(this IEnumerable<AIAgent> agents)
    {
        Guard.NotNull(agents);
        return AgentWorkflowBuilder.BuildSequential(agents);
    }

    public static Workflow BuildQylSequential(this IEnumerable<AIAgent> agents, string workflowName)
    {
        Guard.NotNull(agents);
        Guard.NotNullOrWhiteSpace(workflowName);
        return AgentWorkflowBuilder.BuildSequential(workflowName, agents);
    }

    public static Workflow BuildQylConcurrent(this IEnumerable<AIAgent> agents)
    {
        Guard.NotNull(agents);
        return AgentWorkflowBuilder.BuildConcurrent(agents);
    }

    public static GroupChatWorkflowBuilder BuildQylGroupChat(
        Func<IReadOnlyList<AIAgent>, GroupChatManager> managerFactory)
    {
        Guard.NotNull(managerFactory);
        return AgentWorkflowBuilder.CreateGroupChatBuilderWith(managerFactory);
    }

    public static AIAgent AsQylSequentialAgent(this IEnumerable<AIAgent> agents, string name)
    {
        Guard.NotNull(agents);
        Guard.NotNullOrWhiteSpace(name);
        return AgentWorkflowBuilder.BuildSequential(agents).AsAIAgent(name: name);
    }

    public static AIAgent AsQylConcurrentAgent(this IEnumerable<AIAgent> agents, string name)
    {
        Guard.NotNull(agents);
        Guard.NotNullOrWhiteSpace(name);
        return AgentWorkflowBuilder.BuildConcurrent(agents).AsAIAgent(name: name);
    }

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
