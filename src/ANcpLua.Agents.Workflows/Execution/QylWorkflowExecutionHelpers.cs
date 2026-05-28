using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Workflows.Execution;

internal static class QylWorkflowExecutionHelpers
{
    public static async ValueTask<StreamingRun> StreamQylAgentsAsync(
        Workflow workflow,
        ChatMessage input,
        string? sessionId = null,
        bool emitEvents = true,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(workflow);
        Guard.NotNull(input);

        var run = await InProcessExecution
            .RunStreamingAsync(workflow, input, sessionId, cancellationToken)
            .ConfigureAwait(false);
        await run.TrySendMessageAsync(new TurnToken(emitEvents)).ConfigureAwait(false);
        return run;
    }

    public static async ValueTask<StreamingRun> StreamQylSequentialAsync(
        IEnumerable<AIAgent> agents,
        ChatMessage input,
        string? sessionId = null,
        bool emitEvents = true,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agents);
        Guard.NotNull(input);
        var workflow = AgentWorkflowBuilder.BuildSequential(agents);
        return await StreamQylAgentsAsync(workflow, input, sessionId, emitEvents, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<StreamingRun> StreamQylConcurrentAsync(
        IEnumerable<AIAgent> agents,
        ChatMessage input,
        string? sessionId = null,
        bool emitEvents = true,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agents);
        Guard.NotNull(input);
        var workflow = AgentWorkflowBuilder.BuildConcurrent(agents);
        return await StreamQylAgentsAsync(workflow, input, sessionId, emitEvents, cancellationToken).ConfigureAwait(false);
    }
}
