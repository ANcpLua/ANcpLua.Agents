// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Agents.Testing.Workflows.Environments;

/// <summary>
///     Fluent decorators for <see cref="IWorkflowExecutionEnvironment" />.
///     <para>
///         Time-discipline (<see cref="TimeProvider" />) is intentionally not exposed as a
///         decorator: register the provider directly in DI
///         (<c>services.AddSingleton&lt;TimeProvider&gt;(fakeTime)</c>) and let executors
///         resolve it. <see cref="RecordingExecutionEnvironment" /> accepts a
///         <see cref="TimeProvider" /> in its constructor for audit-timestamping.
///     </para>
/// </summary>
public static class ExecutionEnvironmentExtensions
{
    /// <summary>Wrap with a recorder that captures every <see cref="Workflow" /> dispatch (uses <see cref="TimeProvider.System" />).</summary>
    public static RecordingExecutionEnvironment AsRecording(this IWorkflowExecutionEnvironment environment)
        => new(environment);

    /// <summary>Wrap with a recorder using a caller-supplied <see cref="TimeProvider" /> for deterministic audit timestamps.</summary>
    public static RecordingExecutionEnvironment AsRecording(
        this IWorkflowExecutionEnvironment environment,
        TimeProvider timeProvider)
        => new(environment, timeProvider);
}
