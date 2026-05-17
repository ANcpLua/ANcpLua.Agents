// Copyright (c) Microsoft. All rights reserved.
//
// The single import. Derive WorkflowFixture<TInput>, override BuildWorkflow(),
// and `this.RunAsync(input)` returns a WorkflowRunResult you assert with Should().
// Handles execution environments, checkpoint/resume, and typed event projection.

using AwesomeAssertions;

namespace ANcpLua.Agents.Testing.Workflows;

public abstract class WorkflowFixture<TInput>(ITestOutputHelper output) : IDisposable
    where TInput : notnull
{
    private readonly CancellationTokenSource _cts = new(TimeSpan.FromSeconds(60));
    private CheckpointManager? _checkpointManager;
    private CheckpointInfo? _lastCheckpoint;

    protected ITestOutputHelper Output { get; } = output;

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    /// <summary>Builds the workflow under test. Called once per run.</summary>
    protected abstract Workflow BuildWorkflow();

    /// <summary>Runs the workflow to quiescence under the chosen execution environment.</summary>
    protected Task<WorkflowRunResult> RunAsync(
        TInput input,
        ExecutionEnvironment environment = ExecutionEnvironment.InProcessLockstep)
    {
        return RunCoreAsync(input, environment, false);
    }

    /// <summary>
    ///     Runs the workflow with in-memory checkpointing. The last checkpoint is stored for a subsequent
    ///     <see cref="ResumeAsync" />.
    /// </summary>
    protected Task<WorkflowRunResult> RunWithCheckpointingAsync(
        TInput input,
        ExecutionEnvironment environment = ExecutionEnvironment.InProcessLockstep)
    {
        return RunCoreAsync(input, environment, true);
    }

    /// <summary>Resumes the most recent run from its last checkpoint, optionally pumping an external response.</summary>
    protected async Task<WorkflowRunResult> ResumeAsync(
        ExternalResponse? response = null,
        ExecutionEnvironment environment = ExecutionEnvironment.InProcessLockstep)
    {
        if (_lastCheckpoint is null || _checkpointManager is null)
            throw new InvalidOperationException("Call RunWithCheckpointingAsync before ResumeAsync.");

        var env = environment.ToWorkflowExecutionEnvironment().WithCheckpointing(_checkpointManager);
        await using var run = await env.ResumeStreamingAsync(BuildWorkflow(), _lastCheckpoint, _cts.Token);

        if (response is not null) await run.SendResponseAsync(response);

        var events = await CollectAsync(run, _cts.Token);
        _lastCheckpoint = LatestCheckpoint(events) ?? _lastCheckpoint;
        return new WorkflowRunResult(events, _lastCheckpoint);
    }

    /// <summary>Answers the next pending external request with <paramref name="data" /> and resumes.</summary>
    protected Task<WorkflowRunResult> AnswerRequestAsync(
        WorkflowRunResult pending,
        object data,
        ExecutionEnvironment environment = ExecutionEnvironment.InProcessLockstep)
    {
        var request = pending.PendingRequests.FirstOrDefault()
                      ?? throw new InvalidOperationException("No pending RequestInfoEvent to answer.");
        return ResumeAsync(request.CreateResponse(data), environment);
    }

    private async Task<WorkflowRunResult> RunCoreAsync(TInput input, ExecutionEnvironment environment,
        bool useCheckpointing)
    {
        var env = environment.ToWorkflowExecutionEnvironment();
        if (useCheckpointing)
        {
            _checkpointManager ??= CheckpointManager.CreateInMemory();
            env = env.WithCheckpointing(_checkpointManager);
        }

        await using var run = await env.RunStreamingAsync(BuildWorkflow(), input);
        var events = await CollectAsync(run, _cts.Token);
        _lastCheckpoint = LatestCheckpoint(events);
        return new WorkflowRunResult(events, _lastCheckpoint);
    }

    private static async Task<IReadOnlyList<WorkflowEvent>> CollectAsync(StreamingRun run,
        CancellationToken cancellationToken)
    {
        List<WorkflowEvent> collected = [];
        await foreach (var evt in run.WatchStreamAsync(cancellationToken).WithCancellation(cancellationToken))
            collected.Add(evt);
        return collected;
    }

    private static CheckpointInfo? LatestCheckpoint(IReadOnlyList<WorkflowEvent> events)
    {
        return events.OfType<SuperStepCompletedEvent>()
            .LastOrDefault()?.CompletionInfo?.Checkpoint;
    }
}

/// <summary>
///     Typed projection of a workflow run. Materialized once; every property is a
///     frozen <see cref="IReadOnlyList{T}" />. Call <see cref="Should" /> for fluent
///     assertions.
/// </summary>
public sealed record WorkflowRunResult(IReadOnlyList<WorkflowEvent> Events, CheckpointInfo? LastCheckpoint)
{
    public IReadOnlyList<WorkflowOutputEvent> Outputs { get; } = [.. Events.OfType<WorkflowOutputEvent>()];

    public IReadOnlyList<ExecutorCompletedEvent> CompletedExecutors { get; } =
        [.. Events.OfType<ExecutorCompletedEvent>()];

    public IReadOnlyList<SuperStepCompletedEvent> SuperSteps { get; } = [.. Events.OfType<SuperStepCompletedEvent>()];

    public IReadOnlyList<WorkflowErrorEvent> Errors { get; } = [.. Events.OfType<WorkflowErrorEvent>()];

    public IReadOnlyList<ExternalRequest> PendingRequests { get; }
        = [.. Events.OfType<RequestInfoEvent>().Select(static e => e.Request)];

    /// <summary>Strongly-typed projection of <see cref="Outputs" />. Filters by output payload type.</summary>
    public IReadOnlyList<TOutput> OutputsOf<TOutput>()
        => [.. Outputs.Where(static e => e.Data is TOutput).Select(static e => (TOutput)e.Data!)];

    /// <summary>Pending requests whose payload is of type <typeparamref name="TRequest" />.</summary>
    public IReadOnlyList<ExternalRequest> PendingRequestsOf<TRequest>()
        => [.. PendingRequests.Where(static r => r.Data is TRequest)];

    public WorkflowRunAssertions Should()
    {
        return new WorkflowRunAssertions(this);
    }
}

/// <summary>
///     Fluent assertions. Every method returns `this` so chains read like specs:
///     run.Should().YieldOutput&lt;string&gt;(...).And.HaveNoErrors().And.CompletedExecutors("A","B");
/// </summary>
public readonly struct WorkflowRunAssertions(WorkflowRunResult result)
{
    public WorkflowRunAssertions And => this;

    public WorkflowRunAssertions YieldOutput<TOutput>(Action<TOutput>? assert = null)
    {
        var matches = result.Outputs.Where(e => e.Data is TOutput).Select(e => (TOutput)e.Data!).ToArray();
        matches.Should().NotBeEmpty($"expected at least one WorkflowOutputEvent carrying {typeof(TOutput).Name}");
        if (assert is not null)
            foreach (var value in matches)
                assert(value);

        return this;
    }

    public WorkflowRunAssertions Emit<TEvent>(int? count = null) where TEvent : WorkflowEvent
    {
        var actual = result.Events.OfType<TEvent>().Count();
        if (count is int expected)
            actual.Should().Be(expected);
        else
            actual.Should().BeGreaterThan(0, $"expected at least one {typeof(TEvent).Name}");
        return this;
    }

    public WorkflowRunAssertions NotEmit<TEvent>() where TEvent : WorkflowEvent
    {
        result.Events.OfType<TEvent>().Should().BeEmpty($"expected zero {typeof(TEvent).Name}");
        return this;
    }

    public WorkflowRunAssertions CompletedExecutors(params string[] ids)
    {
        result.CompletedExecutors.Select(e => e.ExecutorId).Should().BeEquivalentTo(ids);
        return this;
    }

    public WorkflowRunAssertions HaveNoErrors()
    {
        result.Errors.Should().BeEmpty("workflow emitted error events");
        return this;
    }

    /// <summary>Asserts a checkpoint is (or isn't) present on the run.</summary>
    public WorkflowRunAssertions HaveLastCheckpoint(bool expected = true)
    {
        if (expected)
        {
            result.LastCheckpoint.Should().NotBeNull("expected the run to have produced a checkpoint");
        }
        else
        {
            result.LastCheckpoint.Should().BeNull("expected the run to have produced no checkpoint");
        }

        return this;
    }

    /// <summary>
    ///     Asserts at least one pending external request whose payload is
    ///     <typeparamref name="TRequest" />, with optional cardinality.
    /// </summary>
    public WorkflowRunAssertions HavePendingRequest<TRequest>(int? count = null)
    {
        var matches = result.PendingRequestsOf<TRequest>();
        if (count is int expected)
        {
            matches.Count.Should().Be(expected, $"expected {expected} pending {typeof(TRequest).Name} request(s)");
        }
        else
        {
            matches.Should().NotBeEmpty($"expected at least one pending request carrying {typeof(TRequest).Name}");
        }

        return this;
    }

    /// <summary>Asserts an exact super-step count.</summary>
    public WorkflowRunAssertions HaveSuperStepCount(int expected)
    {
        result.SuperSteps.Count.Should().Be(expected, $"expected exactly {expected} super-step(s)");
        return this;
    }
}
