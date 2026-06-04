using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Agents.Instrumentation;

/// <summary>
///     Options for Microsoft Agent Framework telemetry middleware.
/// </summary>
public sealed class AgentTelemetryOptions
{
    public const string DefaultActivitySourceName = "ANcpLua.Agents.Instrumentation";
    public const string DefaultMeterName = "ANcpLua.Agents.Instrumentation";

    public string ActivitySourceName { get; set; } = DefaultActivitySourceName;

    public string MeterName { get; set; } = DefaultMeterName;

    public IList<string> FrameworkActivitySourceNames { get; } =
    [
        "Microsoft.Agents.AI",
        "Microsoft.Agents.AI.DurableTask",
        "Experimental.Microsoft.Extensions.AI",
    ];

    public IList<string> FrameworkMeterNames { get; } =
    [
        "Microsoft.Agents.AI",
        "Microsoft.Agents.AI.DurableTask",
        "Experimental.Microsoft.Extensions.AI",
    ];

    public int MaxTagValueLength { get; set; } = 128;

    public static AgentTelemetryOptions Create(Action<AgentTelemetryOptions>? configure = null)
    {
        var options = new AgentTelemetryOptions();
        configure?.Invoke(options);
        options.Validate();
        return options;
    }

    internal void Validate()
    {
        Guard.NotNullOrWhiteSpace(ActivitySourceName);
        Guard.NotNullOrWhiteSpace(MeterName);

        if (MaxTagValueLength is < 16 or > 512)
            throw new InvalidOperationException($"{nameof(MaxTagValueLength)} must be between 16 and 512.");

        if (FrameworkActivitySourceNames.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException($"{nameof(FrameworkActivitySourceNames)} cannot contain empty names.");

        if (FrameworkMeterNames.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException($"{nameof(FrameworkMeterNames)} cannot contain empty names.");
    }
}
