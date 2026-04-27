// Licensed to the .NET Foundation under one or more agreements.

using System.Collections.Concurrent;

namespace ANcpLua.Agents.Testing.Workflows.Environments;

/// <summary>Categorizes which entry-point on <see cref="IWorkflowExecutionEnvironment" /> was hit.</summary>
public enum WorkflowDispatchKind
{
    /// <summary>Mapped to <see cref="IWorkflowExecutionEnvironment.OpenStreamingAsync" />.</summary>
    OpenStreaming,
    /// <summary>Mapped to <see cref="IWorkflowExecutionEnvironment.RunStreamingAsync" />.</summary>
    RunStreaming,
    /// <summary>Mapped to <see cref="IWorkflowExecutionEnvironment.ResumeStreamingAsync" />.</summary>
    ResumeStreaming,
    /// <summary>Mapped to <see cref="IWorkflowExecutionEnvironment.RunAsync" />.</summary>
    Run,
    /// <summary>Mapped to <see cref="IWorkflowExecutionEnvironment.ResumeAsync" />.</summary>
    Resume
}

/// <summary>One observed dispatch through a <see cref="RecordingExecutionEnvironment" />.</summary>
/// <param name="Workflow">Workflow that was dispatched.</param>
/// <param name="Kind">Which entry-point was hit.</param>
/// <param name="InputType">Static input type at the call-site, if applicable.</param>
/// <param name="Input">Boxed input payload, if applicable.</param>
/// <param name="SessionId">Caller-supplied session id, if applicable.</param>
/// <param name="FromCheckpoint">Checkpoint resumed from, if a resume entry-point.</param>
/// <param name="Timestamp">UTC timestamp of dispatch from <see cref="TimeProvider" />.</param>
public sealed record WorkflowDispatchRecord(
    Workflow Workflow,
    WorkflowDispatchKind Kind,
    Type? InputType,
    object? Input,
    string? SessionId,
    CheckpointInfo? FromCheckpoint,
    DateTimeOffset Timestamp);

/// <summary>
///     <see cref="IWorkflowExecutionEnvironment" /> decorator that records every workflow
///     dispatch it forwards. Useful for tests that need to assert "this code path actually
///     executed a workflow" without coupling to <see cref="InProcessExecution" /> internals —
///     the decorator wraps any environment, including environments backed by checkpoint stores
///     or future distributed runners.
///     <para>
///         The recording timestamp is taken from the supplied <see cref="TimeProvider" /> so
///         callers can interpose <c>FakeTimeProvider</c> for deterministic test assertions
///         (default is <see cref="TimeProvider.System" />).
///     </para>
/// </summary>
public sealed class RecordingExecutionEnvironment : IWorkflowExecutionEnvironment
{
    private readonly IWorkflowExecutionEnvironment _inner;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentBag<WorkflowDispatchRecord> _records = [];

    /// <summary>Wrap the given inner environment, timestamping with <see cref="TimeProvider.System" />.</summary>
    public RecordingExecutionEnvironment(IWorkflowExecutionEnvironment inner)
        : this(inner, TimeProvider.System) { }

    /// <summary>Wrap the given inner environment with a caller-supplied <see cref="TimeProvider" />.</summary>
    public RecordingExecutionEnvironment(IWorkflowExecutionEnvironment inner, TimeProvider timeProvider)
    {
        _inner = inner;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public bool IsCheckpointingEnabled => _inner.IsCheckpointingEnabled;

    /// <summary>All workflow dispatches forwarded through this environment, in arrival order.</summary>
    public IReadOnlyCollection<WorkflowDispatchRecord> Records => _records.ToArray();

    /// <inheritdoc />
    public ValueTask<StreamingRun> OpenStreamingAsync(
        Workflow workflow,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        Record(new WorkflowDispatchRecord(workflow, WorkflowDispatchKind.OpenStreaming, InputType: null, Input: null, sessionId, FromCheckpoint: null, _timeProvider.GetUtcNow()));
        return _inner.OpenStreamingAsync(workflow, sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<StreamingRun> RunStreamingAsync<TInput>(
        Workflow workflow,
        TInput input,
        string? sessionId = null,
        CancellationToken cancellationToken = default) where TInput : notnull
    {
        Record(new WorkflowDispatchRecord(workflow, WorkflowDispatchKind.RunStreaming, typeof(TInput), input, sessionId, FromCheckpoint: null, _timeProvider.GetUtcNow()));
        return _inner.RunStreamingAsync(workflow, input, sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<StreamingRun> ResumeStreamingAsync(
        Workflow workflow,
        CheckpointInfo fromCheckpoint,
        CancellationToken cancellationToken = default)
    {
        Record(new WorkflowDispatchRecord(workflow, WorkflowDispatchKind.ResumeStreaming, InputType: null, Input: null, SessionId: null, fromCheckpoint, _timeProvider.GetUtcNow()));
        return _inner.ResumeStreamingAsync(workflow, fromCheckpoint, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Run> RunAsync<TInput>(
        Workflow workflow,
        TInput input,
        string? sessionId = null,
        CancellationToken cancellationToken = default) where TInput : notnull
    {
        Record(new WorkflowDispatchRecord(workflow, WorkflowDispatchKind.Run, typeof(TInput), input, sessionId, FromCheckpoint: null, _timeProvider.GetUtcNow()));
        return _inner.RunAsync(workflow, input, sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Run> ResumeAsync(
        Workflow workflow,
        CheckpointInfo fromCheckpoint,
        CancellationToken cancellationToken = default)
    {
        Record(new WorkflowDispatchRecord(workflow, WorkflowDispatchKind.Resume, InputType: null, Input: null, SessionId: null, fromCheckpoint, _timeProvider.GetUtcNow()));
        return _inner.ResumeAsync(workflow, fromCheckpoint, cancellationToken);
    }

    /// <summary>Reset the recording — useful between scenario steps inside one fixture.</summary>
    public void Clear()
    {
        while (_records.TryTake(out _)) { }
    }

    private void Record(WorkflowDispatchRecord record) => _records.Add(record);
}
