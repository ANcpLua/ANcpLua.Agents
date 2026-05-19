using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ANcpLua.Agents.Mcp.Hosting.Filters;

/// <summary>
/// Per-request scope-constraint injection registered as a call-tool filter on
/// <see cref="IMcpServerBuilder"/>.
/// </summary>
public static class QylMcpScopeInjectionFilter
{
    /// <summary>
    /// Registers a <see cref="McpRequestFilter{TParams,TResult}"/> on the
    /// call-tool pipeline that resolves <typeparamref name="TScope"/> plus its
    /// <see cref="IQylConstraintInjector{TScope}"/> from the per-request
    /// service provider and rewrites the tool's
    /// <see cref="CallToolRequestParams.Arguments"/> bag before dispatching to
    /// the inner handler.
    /// </summary>
    /// <typeparam name="TScope">
    /// The consumer-defined scope type. Resolved from the per-request
    /// <see cref="IServiceProvider"/> exposed on
    /// <see cref="ModelContextProtocol.Server.MessageContext.Services"/>; absence is a no-op.
    /// </typeparam>
    /// <param name="builder">The MCP server builder.</param>
    /// <returns>The same MCP server builder, for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSingleton(QylScope.FromEnvironment());
    /// builder.Services.AddSingleton&lt;IQylConstraintInjector&lt;QylScope&gt;, QylScopeInjector&gt;();
    ///
    /// builder.Services
    ///     .AddMcpServer()
    ///     .WithToolsFromAssembly()
    ///     .WithQylScopeInjection&lt;QylScope&gt;();
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// Both services are resolved from the per-request scope each call so the
    /// filter respects any scoped or transient lifetime the consumer wires up.
    /// When <typeparamref name="TScope"/> is not registered the filter is a
    /// no-op — this keeps the chain composable on hosts where scope wiring is
    /// optional. When <typeparamref name="TScope"/> is registered but
    /// <see cref="IQylConstraintInjector{TScope}"/> is missing the filter
    /// short-circuits the call with <see cref="CallToolResult.IsError"/> set
    /// to <see langword="true"/> and a content block naming the missing
    /// service. (Throwing would leak as a generic "An error occurred invoking
    /// '&lt;tool&gt;'" message because the MCP SDK redacts inner exception
    /// detail — a structured error result preserves the diagnostic.) A
    /// registered scope with no injector means the pipeline cannot enforce
    /// its contract, and silently dropping that constraint would be a
    /// security regression.
    /// </para>
    /// <para>
    /// The filter reads
    /// <see cref="ModelContextProtocol.Server.MessageContext.Services"/> on the
    /// <see cref="ModelContextProtocol.Server.MessageContext"/> base, which the
    /// MCP SDK populates with the per-request scope on every transport (HTTP
    /// streamable-HTTP, stdio, in-memory). Stdio hosts therefore participate
    /// in scope injection identically to HTTP hosts.
    /// </para>
    /// </remarks>
    public static IMcpServerBuilder WithQylScopeInjection<TScope>(this IMcpServerBuilder builder)
        where TScope : class
    {
        Guard.NotNull(builder);

        builder.WithRequestFilters(filters =>
        {
            filters.AddCallToolFilter(next => async (request, cancellationToken) =>
            {
                var services = request.Services;
                if (services is null)
                    return await next(request, cancellationToken);

                var scope = services.GetService<TScope>();
                if (scope is null)
                    return await next(request, cancellationToken);

                var injector = services.GetService<IQylConstraintInjector<TScope>>();
                if (injector is null)
                {
                    return new CallToolResult
                    {
                        IsError = true,
                        Content =
                        [
                            new TextContentBlock
                            {
                                Text =
                                    $"No service for type '{typeof(IQylConstraintInjector<TScope>).FullName}' has been registered. "
                                    + $"Register an IQylConstraintInjector<{typeof(TScope).Name}> alongside the scope itself, "
                                    + "or remove the WithQylScopeInjection chain call."
                            }
                        ]
                    };
                }

                if (request.Params is { } parameters)
                    parameters.Arguments = injector.Inject(parameters.Arguments, scope);

                return await next(request, cancellationToken);
            });
        });

        return builder;
    }
}
