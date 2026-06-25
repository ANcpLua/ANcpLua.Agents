using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ANcpLua.Agents.Instrumentation;

/// <summary>
///     Thin registration helpers over MAF-native OpenTelemetry. MAF 1.11 emits semconv-correct spans
///     itself — <c>UseOpenTelemetry()</c> produces <c>invoke_agent</c> spans, and FunctionInvokingChatClient
///     produces <c>execute_tool</c> spans on the same source — so this package no longer ships hand-rolled
///     run/tool decorators; it registers the framework source/meter and pins sensitive data off.
/// </summary>
public static class AgentTelemetryExtensions
{
    /// <summary>
    ///     MAF agent telemetry source and meter name (<c>OpenTelemetryConsts.DefaultSourceName</c>);
    ///     carries both <c>invoke_agent</c> and <c>execute_tool</c> spans.
    /// </summary>
    public const string AgentFrameworkSourceName = "Experimental.Microsoft.Agents.AI";

    /// <summary>
    ///     Wraps the agent in MAF-native <c>OpenTelemetryAgent</c>, emitting semconv <c>invoke_agent</c> /
    ///     <c>execute_tool</c> spans on <see cref="AgentFrameworkSourceName"/>. Sensitive data is off by
    ///     default; <paramref name="configure"/> runs last and may re-enable it.
    /// </summary>
    public static AIAgentBuilder UseAgentTelemetry(
        this AIAgentBuilder builder,
        Action<OpenTelemetryAgent>? configure = null)
    {
        Guard.NotNull(builder);
        return builder.UseOpenTelemetry(
            AgentFrameworkSourceName,
            agent =>
            {
                agent.EnableSensitiveData = false;
                configure?.Invoke(agent);
            });
    }

    /// <summary>Registers the MAF agent ActivitySource on a <see cref="TracerProviderBuilder"/>.</summary>
    public static TracerProviderBuilder AddAgentFrameworkSources(this TracerProviderBuilder builder)
    {
        Guard.NotNull(builder);
        return builder.AddSource(AgentFrameworkSourceName);
    }

    /// <summary>Registers the MAF agent meter on a <see cref="MeterProviderBuilder"/>.</summary>
    public static MeterProviderBuilder AddAgentFrameworkMeters(this MeterProviderBuilder builder)
    {
        Guard.NotNull(builder);
        return builder.AddMeter(AgentFrameworkSourceName);
    }
}
