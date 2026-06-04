using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Instrumentation;

internal sealed class AgentTelemetryInstrumentation : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<long> _runCount;
    private readonly Histogram<double> _runDuration;
    private readonly Counter<long> _runErrorCount;
    private readonly Counter<long> _toolCallCount;
    private readonly Histogram<double> _toolCallDuration;
    private readonly Counter<long> _toolCallErrorCount;
    private readonly AgentTelemetryOptions _options;

    private AgentTelemetryInstrumentation(AgentTelemetryOptions options)
    {
        _options = options;
        _activitySource = new ActivitySource(options.ActivitySourceName);
        _meter = new Meter(options.MeterName);

        _runCount = _meter.CreateCounter<long>(AgentTelemetryNames.RunCountMetricName);
        _runDuration = _meter.CreateHistogram<double>(AgentTelemetryNames.RunDurationMetricName, "ms");
        _runErrorCount = _meter.CreateCounter<long>(AgentTelemetryNames.RunErrorCountMetricName);
        _toolCallCount = _meter.CreateCounter<long>(AgentTelemetryNames.ToolCallCountMetricName);
        _toolCallDuration = _meter.CreateHistogram<double>(AgentTelemetryNames.ToolCallDurationMetricName, "ms");
        _toolCallErrorCount = _meter.CreateCounter<long>(AgentTelemetryNames.ToolCallErrorCountMetricName);
    }

    public static AgentTelemetryInstrumentation Create(Action<AgentTelemetryOptions>? configure = null) =>
        new(AgentTelemetryOptions.Create(configure));

    public void Dispose()
    {
        _activitySource.Dispose();
        _meter.Dispose();
    }

    public async Task<AgentResponse> TrackRunAsync(
        AIAgent agent,
        Func<Task<AgentResponse>> next)
    {
        var start = Stopwatch.GetTimestamp();
        using var activity = StartActivity(AgentTelemetryNames.RunActivityName, ActivityKind.Internal, agentName: agent.Name);
        var tags = RunTags(agent.Name);

        try
        {
            var response = await next().ConfigureAwait(false);
            CompleteOk(activity, tags);
            _runCount.Add(1, tags);
            return response;
        }
        catch (Exception ex)
        {
            CompleteError(activity, tags, ex);
            _runErrorCount.Add(1, tags);
            throw;
        }
        finally
        {
            _runDuration.Record(ElapsedMilliseconds(start), tags);
        }
    }

    public async IAsyncEnumerable<AgentResponseUpdate> TrackRunStreamingAsync(
        AIAgent agent,
        Func<IAsyncEnumerable<AgentResponseUpdate>> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        using var activity = StartActivity(AgentTelemetryNames.RunActivityName, ActivityKind.Internal, agentName: agent.Name);
        var tags = RunTags(agent.Name);
        var completed = false;

        await using var enumerator = next().GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            AgentResponseUpdate update;
            try
            {
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    completed = true;
                    break;
                }

                update = enumerator.Current;
            }
            catch (Exception ex)
            {
                CompleteError(activity, tags, ex);
                _runErrorCount.Add(1, tags);
                throw;
            }

            yield return update;
        }

        if (completed)
        {
            CompleteOk(activity, tags);
            _runCount.Add(1, tags);
        }

        _runDuration.Record(ElapsedMilliseconds(start), tags);
    }

    public async ValueTask<object?> TrackToolAsync(
        AIAgent agent,
        string? toolName,
        Func<ValueTask<object?>> next)
    {
        var start = Stopwatch.GetTimestamp();
        using var activity = StartActivity(
            AgentTelemetryNames.ToolActivityName,
            ActivityKind.Internal,
            agentName: agent.Name,
            toolName: toolName);
        var tags = ToolTags(agent.Name, toolName);

        try
        {
            var result = await next().ConfigureAwait(false);
            CompleteOk(activity, tags);
            _toolCallCount.Add(1, tags);
            return result;
        }
        catch (Exception ex)
        {
            CompleteError(activity, tags, ex);
            _toolCallErrorCount.Add(1, tags);
            throw;
        }
        finally
        {
            _toolCallDuration.Record(ElapsedMilliseconds(start), tags);
        }
    }

    private Activity? StartActivity(string name, ActivityKind kind, string? agentName, string? toolName = null)
    {
        var activity = _activitySource.StartActivity(name, kind);
        if (activity is null)
            return null;

        activity.SetTag(AgentTelemetryNames.OperationTag, name);
        SetBoundedTag(activity, AgentTelemetryNames.AgentNameTag, agentName);
        SetBoundedTag(activity, AgentTelemetryNames.ToolNameTag, toolName);
        return activity;
    }

    private KeyValuePair<string, object?>[] RunTags(string? agentName) =>
    [
        new(AgentTelemetryNames.OperationTag, AgentTelemetryNames.RunActivityName),
        new(AgentTelemetryNames.AgentNameTag, Bound(agentName)),
        new(AgentTelemetryNames.TelemetryStatusTag, null),
        new(AgentTelemetryNames.ErrorTypeTag, null),
    ];

    private KeyValuePair<string, object?>[] ToolTags(string? agentName, string? toolName) =>
    [
        new(AgentTelemetryNames.OperationTag, AgentTelemetryNames.ToolActivityName),
        new(AgentTelemetryNames.AgentNameTag, Bound(agentName)),
        new(AgentTelemetryNames.ToolNameTag, Bound(toolName)),
        new(AgentTelemetryNames.TelemetryStatusTag, null),
        new(AgentTelemetryNames.ErrorTypeTag, null),
    ];

    private void CompleteOk(Activity? activity, KeyValuePair<string, object?>[] tags)
    {
        activity?.SetTag(AgentTelemetryNames.TelemetryStatusTag, "ok");
        activity?.SetStatus(ActivityStatusCode.Ok);
        SetTag(tags, AgentTelemetryNames.TelemetryStatusTag, "ok");
    }

    private void CompleteError(Activity? activity, KeyValuePair<string, object?>[] tags, Exception exception)
    {
        var errorType = Bound(exception.GetType().Name);
        activity?.SetTag(AgentTelemetryNames.TelemetryStatusTag, "error");
        activity?.SetTag(AgentTelemetryNames.ErrorTypeTag, errorType);
        activity?.SetStatus(ActivityStatusCode.Error);
        SetTag(tags, AgentTelemetryNames.TelemetryStatusTag, "error");
        SetTag(tags, AgentTelemetryNames.ErrorTypeTag, errorType);
    }

    private void SetBoundedTag(Activity activity, string key, string? value)
    {
        var bounded = Bound(value);
        if (bounded is not null)
            activity.SetTag(key, bounded);
    }

    private string? Bound(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= _options.MaxTagValueLength ? trimmed : trimmed[.._options.MaxTagValueLength];
    }

    private static void SetTag(KeyValuePair<string, object?>[] tags, string key, object? value)
    {
        for (var i = 0; i < tags.Length; i++)
        {
            if (tags[i].Key == key)
            {
                tags[i] = new KeyValuePair<string, object?>(key, value);
                return;
            }
        }
    }

    private static double ElapsedMilliseconds(long startTimestamp) =>
        Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
}
