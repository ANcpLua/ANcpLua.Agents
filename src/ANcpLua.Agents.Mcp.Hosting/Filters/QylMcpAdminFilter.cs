using System.Collections.Frozen;
using System.Security.Claims;
using ANcpLua.Roslyn.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ANcpLua.Agents.Mcp.Hosting.Filters;

/// <summary>
/// Admin-role gate registered as a call-tool filter on
/// <see cref="IMcpServerBuilder"/>.
/// </summary>
public static class QylMcpAdminFilter
{
    /// <summary>
    /// Registers a <see cref="McpRequestFilter{TParams,TResult}"/> on the
    /// call-tool pipeline that short-circuits any tool listed in
    /// <see cref="QylAdminFilterOptions.AdminToolNames"/> when the caller's
    /// role set lacks <see cref="QylAdminFilterOptions.RequiredRole"/>.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="configure">
    /// Callback that populates the <see cref="QylAdminFilterOptions"/> bag.
    /// </param>
    /// <returns>The same MCP server builder, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// On denial the filter returns a <see cref="CallToolResult"/> with
    /// <see cref="CallToolResult.IsError"/> set to <see langword="true"/> and
    /// a single <see cref="TextContentBlock"/> describing which role the
    /// caller lacked. The inner handler is never invoked, so the filter is
    /// safe to compose ahead of side-effecting tool logic.
    /// </para>
    /// <para>
    /// When <see cref="QylAdminFilterOptions.ResolveRoles"/> is supplied the
    /// filter resolves the current <see cref="HttpContext"/> via the
    /// registered <see cref="IHttpContextAccessor"/> (added automatically by
    /// this extension) and hands it to the delegate. When the delegate is
    /// <see langword="null"/> the filter falls back to
    /// <see cref="ClaimsPrincipal.FindAll(string)"/> for
    /// <see cref="ClaimTypes.Role"/> on the protocol-level user attached to
    /// the request — which works on both HTTP and stdio transports.
    /// </para>
    /// </remarks>
    public static IMcpServerBuilder WithQylAdminFilter(
        this IMcpServerBuilder builder,
        Action<QylAdminFilterOptions> configure)
    {
        Guard.NotNull(builder);
        Guard.NotNull(configure);

        var options = BuildOptions(configure);

        builder.Services.AddHttpContextAccessor();

        builder.WithRequestFilters(filters =>
        {
            filters.AddCallToolFilter(next => async (request, cancellationToken) =>
            {
                var toolName = request.Params?.Name ?? string.Empty;

                if (!options.AdminToolNames.Contains(toolName))
                    return await next(request, cancellationToken);

                var roles = ResolveCallerRoles(request, options);
                if (roles.Contains(options.RequiredRole))
                    return await next(request, cancellationToken);

                return new CallToolResult
                {
                    IsError = true,
                    Content =
                    [
                        new TextContentBlock
                        {
                            Text =
                                $"Access denied: '{toolName}' requires the '{options.RequiredRole}' role."
                        }
                    ]
                };
            });
        });

        return builder;
    }

    private static QylAdminFilterOptions BuildOptions(Action<QylAdminFilterOptions> configure)
    {
        var options = new QylAdminFilterOptions
        {
            RequiredRole = string.Empty,
            AdminToolNames = FrozenSet<string>.Empty
        };
        configure(options);

        Guard.NotNullOrWhiteSpace(options.RequiredRole, $"{nameof(options)}.{nameof(options.RequiredRole)}");
        Guard.NotNull(options.AdminToolNames, $"{nameof(options)}.{nameof(options.AdminToolNames)}");

        return options;
    }

    private static IReadOnlySet<string> ResolveCallerRoles(
        RequestContext<CallToolRequestParams> request,
        QylAdminFilterOptions options)
    {
        if (options.ResolveRoles is { } resolve)
        {
            var httpContext = request.Services?.GetService<IHttpContextAccessor>()?.HttpContext;
            if (httpContext is not null)
                return resolve(httpContext);
        }

        var principal = request.User;
        if (principal is null)
            return FrozenSet<string>.Empty;

        return principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToFrozenSet(StringComparer.Ordinal);
    }
}
