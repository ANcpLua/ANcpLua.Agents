using ANcpLua.Agents.Instrumentation;
using ANcpLua.Roslyn.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ANcpLua.Agents.Hosting.ServiceDefaults;

/// <summary>
///     Aspire-style service-defaults extensions tuned for Microsoft Agent Framework consumers.
///     Provides health checks plus MAF-native telemetry source/meter registration helpers.
/// </summary>
/// <remarks>
///     Intentionally does NOT configure OTLP exporters, service discovery, or resilience policies
///     — those are opinionated choices best layered by the consumer's own ServiceDefaults helper
///     on top of this one.
/// </remarks>
public static class QylAgentServiceDefaultsExtensions
{
    /// <summary>Canonical MAF agent ActivitySource name — emits both invoke_agent and execute_tool spans.</summary>
    public const string AgentActivitySource = AgentTelemetryExtensions.AgentFrameworkSourceName;

    /// <summary>Canonical MAF DurableTask ActivitySource name.</summary>
    public const string DurableTaskActivitySource = "Microsoft.Agents.AI.DurableTask";

    /// <summary>Canonical Microsoft.Extensions.AI experimental ActivitySource name.</summary>
    public const string ExtensionsActivitySource = "Experimental.Microsoft.Extensions.AI";

    /// <summary>
    ///     Registers default health checks. Wire this from <c>WebApplicationBuilder</c> alongside
    ///     <c>AddOpenTelemetry().WithTracing(t => t.AddQylAgentSources())</c>.
    /// </summary>
    public static IHostApplicationBuilder AddQylAgentServiceDefaults(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);
        builder.Services.AddHealthChecks();
        return builder;
    }

    /// <summary>
    ///     Adds the canonical MAF agent ActivitySource to a <see cref="TracerProviderBuilder"/>. Use
    ///     inside <c>builder.Services.AddOpenTelemetry().WithTracing(t =&gt; t.AddQylAgentSources())</c>.
    /// </summary>
    public static TracerProviderBuilder AddQylAgentSources(this TracerProviderBuilder builder)
    {
        Guard.NotNull(builder);
        return builder.AddAgentFrameworkSources();
    }

    /// <summary>
    ///     Adds the MAF agent meter to a <see cref="MeterProviderBuilder"/>.
    /// </summary>
    public static MeterProviderBuilder AddQylAgentMeters(this MeterProviderBuilder builder)
    {
        Guard.NotNull(builder);
        return builder.AddAgentFrameworkMeters();
    }

    /// <summary>
    ///     Maps the two standard liveness/readiness endpoints used by Aspire / Kubernetes probes:
    ///     <c>/health</c> (runs all registered checks) and <c>/alive</c> (returns 200 if the
    ///     process is up, regardless of checks).
    /// </summary>
    public static IEndpointRouteBuilder MapQylAgentEndpoints(this IEndpointRouteBuilder app)
    {
        Guard.NotNull(app);
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = _ => false });
        return app;
    }
}
