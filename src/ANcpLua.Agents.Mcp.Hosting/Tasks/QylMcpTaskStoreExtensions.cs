using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ANcpLua.Agents.Mcp.Hosting.Tasks;

/// <summary>
/// Task-store wiring for MCP servers that expose long-running tools.
/// </summary>
public static class QylMcpTaskStoreExtensions
{
    /// <summary>
    /// Registers an <see cref="InMemoryMcpTaskStore"/> singleton and wires it onto
    /// <see cref="McpServerOptions.TaskStore"/> so tools annotated with
    /// <c>TaskSupport.Required</c> or <c>TaskSupport.Optional</c> can schedule
    /// background work.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="defaultTtl">
    /// Default time-to-live for completed tasks. Defaults to one hour.
    /// </param>
    /// <param name="maxTtl">
    /// Maximum time-to-live cap. Defaults to six hours.
    /// </param>
    /// <param name="pollInterval">
    /// Advertised polling interval surfaced to clients on new tasks. Defaults to
    /// one second.
    /// </param>
    /// <param name="cleanupInterval">
    /// Interval for background cleanup of expired tasks.
    /// <see langword="null"/> uses the SDK default (one minute). Pass
    /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to disable cleanup.
    /// </param>
    /// <param name="maxTasks">
    /// Maximum number of tasks held in the store globally. Defaults to 500.
    /// </param>
    /// <returns>The same MCP server builder, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The in-memory store loses tasks on restart and does not federate across
    /// replicas. For multi-replica deployments, register a custom
    /// <see cref="IMcpTaskStore"/> implementation directly on
    /// <see cref="IMcpServerBuilder.Services"/> instead of calling this extension.
    /// </para>
    /// </remarks>
    public static IMcpServerBuilder WithInMemoryTaskStore(
        this IMcpServerBuilder builder,
        TimeSpan? defaultTtl = null,
        TimeSpan? maxTtl = null,
        TimeSpan? pollInterval = null,
        TimeSpan? cleanupInterval = null,
        int maxTasks = 500)
    {
        Guard.NotNull(builder);

        builder.Services.AddSingleton<IMcpTaskStore>(_ => new InMemoryMcpTaskStore(
            defaultTtl: defaultTtl ?? TimeSpan.FromHours(1),
            maxTtl: maxTtl ?? TimeSpan.FromHours(6),
            pollInterval: pollInterval ?? TimeSpan.FromSeconds(1),
            cleanupInterval: cleanupInterval,
            maxTasks: maxTasks));

        builder.Services
            .AddOptions<McpServerOptions>()
            .Configure<IMcpTaskStore>((options, store) => options.TaskStore = store);

        return builder;
    }
}
