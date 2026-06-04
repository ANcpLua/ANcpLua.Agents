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
///     Provides health checks plus telemetry registration helpers for the current MAF source
///     family and this repo's instrumentation source.
/// </summary>
/// <remarks>
///     Intentionally does NOT configure OTLP exporters, service discovery, or resilience policies
///     — those are opinionated choices best layered by the consumer's own ServiceDefaults helper
///     on top of this one.
/// </remarks>
public static class QylAgentServiceDefaultsExtensions
{
    /// <summary>Canonical MAF agent ActivitySource name.</summary>
    public const string AgentActivitySource = "Microsoft.Agents.AI";

    /// <summary>Canonical MAF DurableTask ActivitySource name.</summary>
    public const string DurableTaskActivitySource = "Microsoft.Agents.AI.DurableTask";

    /// <summary>Canonical Microsoft.Extensions.AI experimental ActivitySource name.</summary>
    public const string ExtensionsActivitySource = "Experimental.Microsoft.Extensions.AI";

    /// <summary>ActivitySource emitted by ANcpLua.Agents.Instrumentation.</summary>
    public const string InstrumentationActivitySource = AgentTelemetryOptions.DefaultActivitySourceName;

    /// <summary>
    ///     Registers default health checks. Wire this from <c>WebApplicationBuilder</c> alongside
    ///     <c>AddOpenTelemetry().WithTracing(t => t.AddQylAgentSources())</c>.
    /// </summary>
    public static IHostApplicationBuilder AddQylAgentServiceDefaults(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);
        builder.AddAgentTelemetry();
        builder.Services.AddHealthChecks();
        return builder;
    }

    /// <summary>
    ///     Adds both canonical MAF ActivitySources to a <see cref="TracerProviderBuilder"/>. Use
    ///     inside <c>builder.Services.AddOpenTelemetry().WithTracing(t =&gt; t.AddQylAgentSources())</c>.
    /// </summary>
    public static TracerProviderBuilder AddQylAgentSources(this TracerProviderBuilder builder)
    {
        Guard.NotNull(builder);
        return builder.AddAgentFrameworkSources();
    }

    /// <summary>
    ///     Adds MAF and ANcpLua agent meters to a <see cref="MeterProviderBuilder"/>.
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
