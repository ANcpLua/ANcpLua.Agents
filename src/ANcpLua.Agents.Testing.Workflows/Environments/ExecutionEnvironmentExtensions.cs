// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Agents.Testing.Workflows.Environments;

/// <summary>
///     Fluent decorators for <see cref="IWorkflowExecutionEnvironment" /> — compose
///     <see cref="RecordingExecutionEnvironment" /> and <see cref="DeterministicTimeExecutionEnvironment" />
///     in the order needed for the test scenario.
/// </summary>
public static class ExecutionEnvironmentExtensions
{
    /// <summary>Wrap with a recorder that captures every <see cref="Workflow" /> dispatch (uses <see cref="TimeProvider.System" />).</summary>
    public static RecordingExecutionEnvironment AsRecording(this IWorkflowExecutionEnvironment environment)
        => new(environment);

    /// <summary>Wrap with a recorder using a caller-supplied <see cref="TimeProvider" /> for deterministic timestamps.</summary>
    public static RecordingExecutionEnvironment AsRecording(
        this IWorkflowExecutionEnvironment environment,
        TimeProvider timeProvider)
        => new(environment, timeProvider);

    /// <summary>Wrap with a deterministic <see cref="TimeProvider" /> binding.</summary>
    public static DeterministicTimeExecutionEnvironment WithDeterministicTime(
        this IWorkflowExecutionEnvironment environment,
        TimeProvider timeProvider)
        => new(environment, timeProvider);
}
