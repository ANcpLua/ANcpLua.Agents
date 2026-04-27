// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Agents.Testing.Workflows.Environments;

/// <summary>
///     <see cref="IWorkflowExecutionEnvironment" /> decorator that pairs the inner environment
///     with a caller-supplied <see cref="TimeProvider" />. Workflow code that follows the repo
///     convention <c>TimeProvider.System</c> — never <c>DateTime.UtcNow</c> — can be swung onto
///     a deterministic provider by resolving it from DI and binding this environment in the
///     same registration.
///     <para>
///         The decorator only forwards execution; the time-discipline is enforced by the
///         consumer pipeline registering the <see cref="TimeProvider" /> as a singleton.
///         This decorator is the marker that downstream test code uses to discover the
///         deterministic clock without leaking it into production code paths.
///     </para>
/// </summary>
/// <param name="inner">The environment to delegate to.</param>
/// <param name="timeProvider">The deterministic time source bound to this environment.</param>
public sealed class DeterministicTimeExecutionEnvironment(
    IWorkflowExecutionEnvironment inner,
    TimeProvider timeProvider)
    : IWorkflowExecutionEnvironment
{
    /// <summary>The deterministic time provider bound to this environment.</summary>
    public TimeProvider TimeProvider => timeProvider;

    /// <inheritdoc />
    public bool IsCheckpointingEnabled => inner.IsCheckpointingEnabled;

    /// <inheritdoc />
    public ValueTask<StreamingRun> OpenStreamingAsync(
        Workflow workflow,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
        => inner.OpenStreamingAsync(workflow, sessionId, cancellationToken);

    /// <inheritdoc />
    public ValueTask<StreamingRun> RunStreamingAsync<TInput>(
        Workflow workflow,
        TInput input,
        string? sessionId = null,
        CancellationToken cancellationToken = default) where TInput : notnull
        => inner.RunStreamingAsync(workflow, input, sessionId, cancellationToken);

    /// <inheritdoc />
    public ValueTask<StreamingRun> ResumeStreamingAsync(
        Workflow workflow,
        CheckpointInfo fromCheckpoint,
        CancellationToken cancellationToken = default)
        => inner.ResumeStreamingAsync(workflow, fromCheckpoint, cancellationToken);

    /// <inheritdoc />
    public ValueTask<Run> RunAsync<TInput>(
        Workflow workflow,
        TInput input,
        string? sessionId = null,
        CancellationToken cancellationToken = default) where TInput : notnull
        => inner.RunAsync(workflow, input, sessionId, cancellationToken);

    /// <inheritdoc />
    public ValueTask<Run> ResumeAsync(
        Workflow workflow,
        CheckpointInfo fromCheckpoint,
        CancellationToken cancellationToken = default)
        => inner.ResumeAsync(workflow, fromCheckpoint, cancellationToken);
}
