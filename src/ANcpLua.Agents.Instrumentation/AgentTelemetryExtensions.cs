using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ANcpLua.Agents.Instrumentation;

/// <summary>
///     Registration and middleware extensions for Agent Framework telemetry.
/// </summary>
public static class AgentTelemetryExtensions
{
    public static IHostApplicationBuilder AddAgentTelemetry(
        this IHostApplicationBuilder builder,
        Action<AgentTelemetryOptions>? configure = null)
    {
        Guard.NotNull(builder);
        builder.Services.AddSingleton(AgentTelemetryOptions.Create(configure));
        return builder;
    }

    public static TracerProviderBuilder AddAgentFrameworkSources(
        this TracerProviderBuilder builder,
        Action<AgentTelemetryOptions>? configure = null)
    {
        Guard.NotNull(builder);
        var options = AgentTelemetryOptions.Create(configure);
        return builder.AddSource(SourceNames(options));
    }

    public static MeterProviderBuilder AddAgentFrameworkMeters(
        this MeterProviderBuilder builder,
        Action<AgentTelemetryOptions>? configure = null)
    {
        Guard.NotNull(builder);
        var options = AgentTelemetryOptions.Create(configure);
        return builder.AddMeter(MeterNames(options));
    }

    public static AIAgentBuilder UseAgentRunTelemetry(
        this AIAgentBuilder builder,
        Action<AgentTelemetryOptions>? configure = null)
    {
        Guard.NotNull(builder);
        return builder.Use(innerAgent => new AgentRunTelemetryAgent(innerAgent, configure));
    }

    public static AIAgentBuilder UseAgentToolTelemetry(
        this AIAgentBuilder builder,
        Action<AgentTelemetryOptions>? configure = null)
    {
        Guard.NotNull(builder);
        return builder.Use(innerAgent => new AgentToolTelemetryAgent(innerAgent, configure));
    }

    private static string[] SourceNames(AgentTelemetryOptions options) =>
    [
        options.ActivitySourceName,
        .. options.FrameworkActivitySourceNames,
    ];

    private static string[] MeterNames(AgentTelemetryOptions options) =>
    [
        options.MeterName,
        .. options.FrameworkMeterNames,
    ];
}
