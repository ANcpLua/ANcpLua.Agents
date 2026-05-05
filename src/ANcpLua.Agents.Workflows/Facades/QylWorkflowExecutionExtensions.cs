using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Agents.Workflows;

public static class QylWorkflowExecutionExtensions
{
    public static string ToQylMermaidString(this Workflow workflow)
    {
        Guard.NotNull(workflow);

        return workflow.ToMermaidString();
    }

    public static ValueTask<StreamingRun> RunQylStreamingAsync<TInput>(
        this Workflow workflow,
        TInput input,
        string? runId = null,
        CancellationToken cancellationToken = default)
        where TInput : notnull
    {
        Guard.NotNull(workflow);

        return InProcessExecution.RunStreamingAsync(workflow, input, runId, cancellationToken);
    }

    public static ValueTask<StreamingRun> RunQylStreamingAsync<TInput>(
        this Workflow workflow,
        TInput input,
        CheckpointManager checkpointManager,
        string? runId = null,
        CancellationToken cancellationToken = default)
        where TInput : notnull
    {
        Guard.NotNull(workflow);
        Guard.NotNull(checkpointManager);

        return InProcessExecution.RunStreamingAsync(workflow, input, checkpointManager, runId, cancellationToken);
    }
}
