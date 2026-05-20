using ANcpLua.Roslyn.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Mcp.Hosting;

/// <summary>
/// Endpoint-mapping facade over MAF / ModelContextProtocol.AspNetCore.
/// </summary>
/// <remarks>
/// The library does not ship an <c>AddQylMcpServer</c> sibling to the SDK's
/// <c>services.AddMcpServer()</c>. The canonical SDK chain
/// <c>services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly()</c>
/// is the entry point. All qyl-specific composition belongs on
/// <c>IMcpServerBuilder</c> as <c>WithX(...)</c> chain calls (see the
/// <c>Filters</c>, <c>Authentication</c>, <c>Logging</c>, and <c>Tasks</c>
/// folders).
/// </remarks>
public static class QylMcpServerExtensions
{
    /// <summary>
    /// Maps the MCP Streamable-HTTP endpoint at the given path and, by default,
    /// the qyl-shaped <c>/alive</c> + <c>/health</c> health-check endpoints
    /// alongside it.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The path to expose the MCP endpoint at. Defaults to <c>/mcp</c>.</param>
    /// <param name="mapHealthEndpoints">
    /// When <see langword="true"/> (the default), also maps
    /// <c>/alive</c> filtered to checks tagged <c>"live"</c> and
    /// <c>/health</c> filtered to checks tagged <c>"ready"</c>. When
    /// <see langword="false"/>, only the MCP endpoint is mapped — callers that
    /// want different health-endpoint paths or predicates wire those up
    /// themselves.
    /// </param>
    /// <returns>The endpoint convention builder for the mapped MCP route.</returns>
    /// <remarks>
    /// <para>
    /// The convention builder returned applies only to the MCP route itself —
    /// the health-check routes are mapped as siblings and have their own
    /// conventions. This matches the qyl host shape where
    /// <c>RequireAuthorization()</c> is applied to MCP only, leaving the
    /// health endpoints anonymous for liveness/readiness probes.
    /// </para>
    /// <para>
    /// The two tag predicates mirror the convention used by
    /// <c>qyl.collector</c>: <c>"live"</c> is reserved for cheap in-process
    /// liveness signals, <c>"ready"</c> for dependency-readiness signals. The
    /// caller is responsible for registering the checks themselves via
    /// <see cref="HealthCheckServiceCollectionExtensions.AddHealthChecks(IServiceCollection)"/>
    /// with the matching tags.
    /// </para>
    /// </remarks>
    public static IEndpointConventionBuilder MapQylMcp(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/mcp",
        bool mapHealthEndpoints = true)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(pattern);

        if (mapHealthEndpoints)
        {
            endpoints.MapHealthChecks(
                "/alive",
                new HealthCheckOptions { Predicate = static check => check.Tags.Contains("live") });
            endpoints.MapHealthChecks(
                "/health",
                new HealthCheckOptions { Predicate = static check => check.Tags.Contains("ready") });
        }

        return endpoints.MapMcp(pattern);
    }
}
