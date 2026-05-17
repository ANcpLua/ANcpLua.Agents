using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows.InProc;

namespace ANcpLua.Agents.Testing.Workflows;

/// <summary>
///     Entry point for focused workflow run tests. <see cref="WorkflowFixture{TInput}" />
///     remains the fixture-inheritance path; this harness covers direct arrange/run/assert cases.
/// </summary>
public static class WorkflowRunHarness
{
    /// <summary>Creates a run harness builder from a workflow factory.</summary>
    public static WorkflowRunHarnessBuilder For(Func<Workflow> workflowFactory)
    {
        Guard.NotNull(workflowFactory);
        return new WorkflowRunHarnessBuilder(workflowFactory);
    }
}

/// <summary>Untyped workflow run harness builder. Call <see cref="WithInput{TInput}" /> to continue.</summary>
public sealed class WorkflowRunHarnessBuilder
{
    private readonly Func<Workflow> _workflowFactory;

    internal WorkflowRunHarnessBuilder(Func<Workflow> workflowFactory)
    {
        _workflowFactory = workflowFactory;
    }

    /// <summary>Sets the workflow input and returns a typed run harness builder.</summary>
    public WorkflowRunHarnessBuilder<TInput> WithInput<TInput>(TInput input)
        where TInput : notnull
    {
        return new WorkflowRunHarnessBuilder<TInput>(_workflowFactory, input);
    }
}

/// <summary>Fluent builder for running a single workflow turn.</summary>
public sealed class WorkflowRunHarnessBuilder<TInput>
    where TInput : notnull
{
    private readonly TInput _input;
    private readonly Func<Workflow> _workflowFactory;
    private ExecutionEnvironment _environment = ExecutionEnvironment.InProcess_Lockstep;
    private bool _useCheckpointing;

    internal WorkflowRunHarnessBuilder(Func<Workflow> workflowFactory, TInput input)
    {
        _workflowFactory = workflowFactory;
        _input = input;
    }

    /// <summary>Chooses the in-process execution environment for the run.</summary>
    public WorkflowRunHarnessBuilder<TInput> In(ExecutionEnvironment environment)
    {
        _environment = environment;
        return this;
    }

    /// <summary>Enables in-memory checkpointing so the result can be resumed.</summary>
    public WorkflowRunHarnessBuilder<TInput> WithCheckpointing()
    {
        _useCheckpointing = true;
        return this;
    }

    /// <summary>Runs the workflow and materializes its event stream.</summary>
    public async Task<WorkflowRunHarnessResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var workflow = _workflowFactory();
        var checkpointManager = _useCheckpointing ? CheckpointManager.CreateInMemory() : null;
        var environment = WorkflowRunHarnessRunner.CreateEnvironment(_environment, checkpointManager);

        await using var run = await environment.RunStreamingAsync(workflow, _input, cancellationToken: cancellationToken);
        var events = await WorkflowRunHarnessRunner.CollectAsync(run, cancellationToken).ConfigureAwait(false);
        var result = new WorkflowRunResult(events, WorkflowRunHarnessRunner.LatestCheckpoint(events));

        return new WorkflowRunHarnessResult(_workflowFactory, _environment, checkpointManager, result);
    }
}

/// <summary>
///     Materialized workflow run harness result with convenience accessors and optional checkpoint resume.
/// </summary>
public sealed class WorkflowRunHarnessResult
{
    private readonly CheckpointManager? _checkpointManager;
    private readonly ExecutionEnvironment _environment;
    private readonly Func<Workflow> _workflowFactory;

    internal WorkflowRunHarnessResult(
        Func<Workflow> workflowFactory,
        ExecutionEnvironment environment,
        CheckpointManager? checkpointManager,
        WorkflowRunResult result)
    {
        _workflowFactory = workflowFactory;
        _environment = environment;
        _checkpointManager = checkpointManager;
        Result = result;
    }

    /// <summary>The underlying workflow run result used by existing workflow assertions.</summary>
    public WorkflowRunResult Result { get; }

    /// <summary>All workflow events produced by the run.</summary>
    public IReadOnlyList<WorkflowEvent> Events => Result.Events;

    /// <summary>Workflow output events produced by the run.</summary>
    public IReadOnlyList<WorkflowOutputEvent> Outputs => Result.Outputs;

    /// <summary>Executor completion events produced by the run.</summary>
    public IReadOnlyList<ExecutorCompletedEvent> CompletedExecutors => Result.CompletedExecutors;

    /// <summary>Completed super-step events produced by the run.</summary>
    public IReadOnlyList<SuperStepCompletedEvent> SuperSteps => Result.SuperSteps;

    /// <summary>Workflow error events produced by the run.</summary>
    public IReadOnlyList<WorkflowErrorEvent> Errors => Result.Errors;

    /// <summary>Pending external requests captured at the end of the run.</summary>
    public IReadOnlyList<ExternalRequest> PendingRequests => Result.PendingRequests;

    /// <summary>The last checkpoint emitted by the run, or <see langword="null"/> when no checkpoint was emitted.</summary>
    public CheckpointInfo? LastCheckpoint => Result.LastCheckpoint;

    /// <summary>Starts the existing fluent assertions over the underlying result.</summary>
    public WorkflowRunAssertions Should()
    {
        return Result.Should();
    }

    /// <summary>Answers the first pending external request and resumes from the last checkpoint.</summary>
    public Task<WorkflowRunHarnessResult> ResumeWithAsync(
        object data,
        CancellationToken cancellationToken = default)
    {
        var request = PendingRequests.FirstOrDefault()
                      ?? throw new InvalidOperationException("No pending RequestInfoEvent to answer.");

        return ResumeAsync(request.CreateResponse(data), cancellationToken);
    }

    /// <summary>Resumes the workflow from the last checkpoint with an explicit external response.</summary>
    public async Task<WorkflowRunHarnessResult> ResumeAsync(
        ExternalResponse response,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(response);

        if (_checkpointManager is null || LastCheckpoint is null)
            throw new InvalidOperationException("Run the harness with checkpointing before resuming.");

        var environment = WorkflowRunHarnessRunner.CreateEnvironment(_environment, _checkpointManager);
        await using var run = await environment.ResumeStreamingAsync(_workflowFactory(), LastCheckpoint, cancellationToken);

        await run.SendResponseAsync(response).ConfigureAwait(false);

        var events = await WorkflowRunHarnessRunner.CollectAsync(run, cancellationToken).ConfigureAwait(false);
        var result = new WorkflowRunResult(events, WorkflowRunHarnessRunner.LatestCheckpoint(events) ?? LastCheckpoint);

        return new WorkflowRunHarnessResult(_workflowFactory, _environment, _checkpointManager, result);
    }
}

internal static class WorkflowRunHarnessRunner
{
    public static InProcessExecutionEnvironment CreateEnvironment(
        ExecutionEnvironment environment,
        CheckpointManager? checkpointManager)
    {
        var executionEnvironment = environment.ToWorkflowExecutionEnvironment();
        return checkpointManager is null
            ? executionEnvironment
            : executionEnvironment.WithCheckpointing(checkpointManager);
    }

    public static async Task<IReadOnlyList<WorkflowEvent>> CollectAsync(
        StreamingRun run,
        CancellationToken cancellationToken)
    {
        List<WorkflowEvent> collected = [];
        await foreach (var evt in run.WatchStreamAsync(cancellationToken))
            collected.Add(evt);

        return collected;
    }

    public static CheckpointInfo? LatestCheckpoint(IReadOnlyList<WorkflowEvent> events)
    {
        return events.OfType<SuperStepCompletedEvent>()
            .LastOrDefault()?.CompletionInfo?.Checkpoint;
    }
}
