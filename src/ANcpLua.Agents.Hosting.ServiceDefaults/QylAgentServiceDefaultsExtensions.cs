using ANcpLua.Roslyn.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace ANcpLua.Agents.Hosting.ServiceDefaults;

/// <summary>
///     Aspire-style service-defaults extensions tuned for Microsoft Agent Framework consumers.
///     Provides health checks + ActivitySource registration helpers for the two canonical MAF
///     trace sources (<c>Microsoft.Agents.AI</c>, <c>Experimental.Microsoft.Extensions.AI</c>).
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
    ///     Adds both canonical MAF ActivitySources to a <see cref="TracerProviderBuilder"/>. Use
    ///     inside <c>builder.Services.AddOpenTelemetry().WithTracing(t =&gt; t.AddQylAgentSources())</c>.
    /// </summary>
    public static TracerProviderBuilder AddQylAgentSources(this TracerProviderBuilder builder)
    {
        Guard.NotNull(builder);
        return builder.AddSource(AgentActivitySource, ExtensionsActivitySource);
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
