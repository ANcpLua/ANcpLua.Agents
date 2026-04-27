// Licensed to the .NET Foundation under one or more agreements.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace ANcpLua.Agents.Testing.Conformance.Telemetry;

/// <summary>
///     Capture scope for <see cref="Activity" /> spans emitted from one or more
///     <see cref="ActivitySource" />s during a test body. Disposes the underlying
///     <see cref="ActivityListener" /> deterministically so process-globality of the
///     <see cref="ActivitySource" /> registry does not leak across parallel tests.
///     <para>
///         Recorded activities are exposed via <see cref="StoppedActivities" /> after
///         <c>Activity.Stop</c> fires — that is the canonical "completed" signal for
///         downstream OTel exporters, so asserting on stopped activities matches what
///         a production exporter would actually see.
///     </para>
/// </summary>
public sealed class CapturedTelemetry : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly ConcurrentBag<Activity> _started = [];
    private readonly ConcurrentBag<Activity> _stopped = [];

    private CapturedTelemetry(IReadOnlyCollection<string> sourceNames)
    {
        var names = new HashSet<string>(sourceNames, StringComparer.Ordinal);
        _listener = new ActivityListener
        {
            ShouldListenTo = source => names.Contains(source.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => _started.Add(activity),
            ActivityStopped = activity => _stopped.Add(activity)
        };

        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>All activities observed in <c>ActivityStarted</c>, regardless of completion.</summary>
    public IReadOnlyCollection<Activity> StartedActivities => _started.ToArray();

    /// <summary>Activities that completed before the scope was disposed. The canonical exporter view.</summary>
    public IReadOnlyCollection<Activity> StoppedActivities => _stopped.ToArray();

    /// <summary>Capture activities from one or more sources by name (exact match, ordinal).</summary>
    public static CapturedTelemetry FromSources(params string[] sourceNames)
        => new(sourceNames);

    /// <inheritdoc />
    public void Dispose() => _listener.Dispose();
}
