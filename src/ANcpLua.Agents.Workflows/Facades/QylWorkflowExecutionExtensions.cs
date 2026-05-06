using System.Diagnostics;
using ANcpLua.Agents.Workflows.Execution;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Observability;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Workflows;

/// <summary>
/// Qyl-prefixed facades over core MAF workflow execution APIs.
/// </summary>
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

    /// <summary>
    /// Renders the workflow as a Mermaid graph.
    /// </summary>
    /// <param name="workflow">The workflow to render.</param>
    /// <returns>The Mermaid graph text.</returns>
    public static string ToQylMermaidString(this Workflow workflow)
    {
        Guard.NotNull(workflow);

        return workflow.ToMermaidString();
    }

    /// <summary>
    ///     Runs the workflow once, returning the terminal <see cref="Run" />.
    /// </summary>
    /// <typeparam name="TInput">The workflow input type.</typeparam>
    /// <param name="workflow">The workflow to run.</param>
    /// <param name="input">The workflow input.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="cancellationToken">Cancellation token for the run.</param>
    /// <returns>The completed workflow run.</returns>
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
    /// <typeparam name="TInput">The workflow input type.</typeparam>
    /// <param name="workflow">The workflow to run.</param>
    /// <param name="input">The workflow input.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
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
    /// <typeparam name="TInput">The workflow input type.</typeparam>
    /// <param name="workflow">The workflow to run.</param>
    /// <param name="input">The workflow input.</param>
    /// <param name="checkpointManager">Checkpoint manager for the run.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
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
    /// <param name="workflow">The workflow to resume.</param>
    /// <param name="from">Checkpoint to resume from.</param>
    /// <param name="checkpointManager">Checkpoint manager for the run.</param>
    /// <param name="cancellationToken">Cancellation token for resuming the stream.</param>
    /// <returns>The resumed streaming workflow run.</returns>
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
    /// <param name="workflow">The workflow to expose.</param>
    /// <param name="id">Optional agent id.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
    /// <param name="executionEnvironment">Optional workflow execution environment.</param>
    /// <param name="includeExceptionDetails">Whether exception details are included in responses.</param>
    /// <param name="includeWorkflowOutputsInResponse">Whether workflow outputs are included in responses.</param>
    /// <returns>The workflow exposed as an agent.</returns>
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
    /// <param name="workflow">The workflow to bind.</param>
    /// <param name="id">The executor id.</param>
    /// <param name="options">Optional executor options.</param>
    /// <returns>The executor binding for the workflow.</returns>
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
    /// <param name="workflow">The workflow to run.</param>
    /// <param name="input">The chat-message input.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="emitEvents">Whether to emit workflow events.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
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
    /// <param name="workflow">The workflow to run.</param>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="emitEvents">Whether to emit workflow events.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
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

    /// <summary>
    /// Starts a workflow run and returns the live stream.
    /// </summary>
    /// <typeparam name="TInput">The workflow input type.</typeparam>
    /// <param name="workflow">The workflow to run.</param>
    /// <param name="input">The workflow input.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
    public static ValueTask<StreamingRun> RunQylStreamingAsync<TInput>(
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
    /// Starts a checkpointed workflow run and returns the live stream.
    /// </summary>
    /// <typeparam name="TInput">The workflow input type.</typeparam>
    /// <param name="workflow">The workflow to run.</param>
    /// <param name="input">The workflow input.</param>
    /// <param name="checkpointManager">Checkpoint manager for the run.</param>
    /// <param name="sessionId">Optional workflow session id.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
    public static ValueTask<StreamingRun> RunQylStreamingAsync<TInput>(
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
}
