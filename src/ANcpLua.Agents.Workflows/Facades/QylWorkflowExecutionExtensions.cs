using System.Diagnostics;
using ANcpLua.Agents.Workflows.Execution;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Observability;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Workflows;

public static class QylWorkflowExecutionExtensions
{
    /// <summary>
    ///     Enables OpenTelemetry instrumentation on the workflow being built.
    /// </summary>
    public static WorkflowBuilder WithQylTelemetry(
        this WorkflowBuilder builder,
        Action<WorkflowTelemetryOptions>? configure = null,
        ActivitySource? activitySource = null)
    {
        Guard.NotNull(builder);
        return builder.WithOpenTelemetry(configure, activitySource);
    }

    public static string ToQylMermaidString(this Workflow workflow)
    {
        Guard.NotNull(workflow);

        return workflow.ToMermaidString();
    }

    /// <summary>
    ///     Runs the workflow once, returning the terminal <see cref="Run" />.
    /// </summary>
    public static ValueTask<Run> RunQylAsync<TInput>(
        this Workflow workflow,
        TInput input,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
        where TInput : notnull
    {
        Guard.NotNull(workflow);
        Guard.NotNull(input);
        return InProcessExecution.RunAsync(workflow, input, sessionId, cancellationToken);
    }

    /// <summary>
    ///     Starts a workflow run and returns the live stream.
    /// </summary>
    public static ValueTask<StreamingRun> StreamQylAsync<TInput>(
        this Workflow workflow,
        TInput input,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
        where TInput : notnull
    {
        Guard.NotNull(workflow);
        Guard.NotNull(input);
        return InProcessExecution.RunStreamingAsync(workflow, input, sessionId, cancellationToken);
    }

    /// <summary>
    ///     Starts a checkpointed workflow run and returns the live stream.
    /// </summary>
    public static ValueTask<StreamingRun> StreamQylCheckpointedAsync<TInput>(
        this Workflow workflow,
        TInput input,
        CheckpointManager checkpointManager,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
        where TInput : notnull
    {
        Guard.NotNull(workflow);
        Guard.NotNull(input);
        Guard.NotNull(checkpointManager);
        return InProcessExecution.RunStreamingAsync(workflow, input, checkpointManager, sessionId, cancellationToken);
    }

    /// <summary>
    ///     Resumes a checkpointed workflow stream.
    /// </summary>
    public static ValueTask<StreamingRun> ResumeQylAsync(
        this Workflow workflow,
        CheckpointInfo from,
        CheckpointManager checkpointManager,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(workflow);
        Guard.NotNull(from);
        Guard.NotNull(checkpointManager);
        return InProcessExecution.ResumeStreamingAsync(workflow, from, checkpointManager, cancellationToken);
    }

    /// <summary>
    ///     Surfaces the workflow as an <see cref="AIAgent" />.
    /// </summary>
    public static AIAgent AsQylAIAgent(
        this Workflow workflow,
        string? id = null,
        string? name = null,
        string? description = null,
        IWorkflowExecutionEnvironment? executionEnvironment = null,
        bool includeExceptionDetails = false,
        bool includeWorkflowOutputsInResponse = false)
    {
        Guard.NotNull(workflow);
        return workflow.AsAIAgent(
            id,
            name,
            description,
            executionEnvironment,
            includeExceptionDetails,
            includeWorkflowOutputsInResponse);
    }

    /// <summary>
    ///     Binds the workflow as an in-process sub-workflow executor.
    /// </summary>
    public static ExecutorBinding BindAsQylSubWorkflow(
        this Workflow workflow,
        string id,
        ExecutorOptions? options = null)
    {
        Guard.NotNull(workflow);
        Guard.NotNullOrWhiteSpace(id);
        return workflow.BindAsExecutor(id, options);
    }

    /// <summary>
    ///     Streams a workflow and pumps a <see cref="TurnToken" /> to activate
    ///     <see cref="AIAgent" /> executors.
    /// </summary>
    public static ValueTask<StreamingRun> StreamQylAgentsAsync(
        this Workflow workflow,
        ChatMessage input,
        string? sessionId = null,
        bool emitEvents = true,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(workflow);
        Guard.NotNull(input);
        return QylWorkflowExecutionHelpers.StreamQylAgentsAsync(workflow, input, sessionId, emitEvents, cancellationToken);
    }

    /// <summary>
    ///     Convenience overload for <c>StreamQylAgentsAsync</c>.
    /// </summary>
    public static ValueTask<StreamingRun> StreamQylAgentsAsync(
        this Workflow workflow,
        string prompt,
        string? sessionId = null,
        bool emitEvents = true,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(workflow);
        Guard.NotNullOrWhiteSpace(prompt);
        return QylWorkflowExecutionHelpers.StreamQylAgentsAsync(
            workflow,
            new ChatMessage(ChatRole.User, prompt),
            sessionId,
            emitEvents,
            cancellationToken);
    }

    public static ValueTask<StreamingRun> RunQylStreamingAsync<TInput>(
        this Workflow workflow,
        TInput input,
        string? runId = null,
        CancellationToken cancellationToken = default)
        where TInput : notnull
    {
        Guard.NotNull(workflow);
        Guard.NotNull(input);
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
