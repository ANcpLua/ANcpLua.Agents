using ANcpLua.Roslyn.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Mcp.Hosting;

/// <summary>
/// Qyl-prefixed facades over MAF / ModelContextProtocol.AspNetCore server-hosting APIs.
/// </summary>
public static class QylMcpServerExtensions
{
    /// <summary>
    /// Registers an MCP server with HTTP transport and discovers tools from the calling
    /// assembly (types annotated with <c>[McpServerToolType]</c>).
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylMcpServer(this IServiceCollection services)
    {
        Guard.NotNull(services);

        services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();

        return services;
    }

    /// <summary>
    /// Maps the MCP Streamable-HTTP endpoint at the given path.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The path to expose the MCP endpoint at. Defaults to <c>/mcp</c>.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylMcp(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/mcp")
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(pattern);

        return endpoints.MapMcp(pattern);
    }
}
