// Licensed to the .NET Foundation under one or more agreements.

using System.Diagnostics;

namespace ANcpLua.Agents.Testing.Hosting.Internal;

/// <summary>
///     Test-local <see cref="ActivityListener" /> bound to a single source name. Disposes
///     the listener so the process-global <see cref="ActivitySource" /> registry does not
///     leak across parallel tests.
///     <para>
///         When <see cref="ActivitySource.AddActivityListener" /> is called, the runtime
///         invokes <c>ShouldListenTo</c> once for every previously created
///         <see cref="ActivitySource" /> in the process — that callback is also where this
///         scope flips <see cref="SourceCreated" /> to <c>true</c> on a name match. New
///         sources created later trigger the same callback again.
///     </para>
/// </summary>
internal sealed class ActivityListenerScope : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly string _sourceName;
    private int _capturedCount;
    private int _sourceCreatedFlag;

    private ActivityListenerScope(string sourceName)
    {
        _sourceName = sourceName;
        _listener = new ActivityListener
        {
            ShouldListenTo = OnShouldListenTo,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => Interlocked.Increment(ref _capturedCount)
        };

        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>Number of activities captured for the bound source since the scope was created.</summary>
    public int CapturedActivityCount => Volatile.Read(ref _capturedCount);

    /// <summary>True once an <see cref="ActivitySource" /> with the bound name has been seen by the runtime.</summary>
    public bool SourceCreated => Volatile.Read(ref _sourceCreatedFlag) != 0;

    /// <summary>Create a listener bound to <paramref name="sourceName" />.</summary>
    public static ActivityListenerScope ForSource(string sourceName)
        => new(sourceName);

    /// <inheritdoc />
    public void Dispose() => _listener.Dispose();

    private bool OnShouldListenTo(ActivitySource source)
    {
        if (!string.Equals(source.Name, _sourceName, StringComparison.Ordinal))
            return false;

        Interlocked.Exchange(ref _sourceCreatedFlag, 1);
        return true;
    }
}
