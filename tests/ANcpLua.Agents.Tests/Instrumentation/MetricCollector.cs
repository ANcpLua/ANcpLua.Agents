using System.Diagnostics.Metrics;

namespace ANcpLua.Agents.Tests.Instrumentation;

internal sealed class MetricCollector : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly List<LongMeasurement> _longMeasurements = [];
    private readonly List<DoubleMeasurement> _doubleMeasurements = [];
    private readonly string _meterName;

    public MetricCollector(string meterName)
    {
        _meterName = meterName;
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == _meterName)
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            _longMeasurements.Add(new LongMeasurement(instrument.Name, measurement, Tags(tags))));
        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            _doubleMeasurements.Add(new DoubleMeasurement(instrument.Name, measurement, Tags(tags))));
        _listener.Start();
    }

    public LongMeasurement SingleLong(string instrumentName)
    {
        var matches = _longMeasurements.Where(measurement => measurement.InstrumentName == instrumentName).ToArray();
        matches.Should().ContainSingle();
        return matches[0];
    }

    public DoubleMeasurement SingleDouble(string instrumentName)
    {
        var matches = _doubleMeasurements.Where(measurement => measurement.InstrumentName == instrumentName).ToArray();
        matches.Should().ContainSingle();
        return matches[0];
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    private static IReadOnlyDictionary<string, object?> Tags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        Dictionary<string, object?> values = new(StringComparer.Ordinal);
        foreach (var tag in tags)
            values[tag.Key] = tag.Value;
        return values;
    }

    public sealed record LongMeasurement(string InstrumentName, long Value, IReadOnlyDictionary<string, object?> Tags);

    public sealed record DoubleMeasurement(string InstrumentName, double Value, IReadOnlyDictionary<string, object?> Tags);
}
