using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ANcpLua.Agents.Mcp.Hosting.Tasks;

/// <summary>
/// Task-store wiring for MCP servers that expose long-running tools.
/// </summary>
[Experimental("MCPEXP001")]
public static class QylMcpTaskStoreExtensions
{
    /// <summary>
    /// Registers an <see cref="InMemoryMcpTaskStore"/> singleton, wires it onto
    /// <see cref="McpServerOptions.TaskStore"/> so tools annotated with
    /// <c>TaskSupport.Required</c> or <c>TaskSupport.Optional</c> can schedule
    /// background work, and sets <see cref="McpServerOptions.SendTaskStatusNotifications"/>.
    /// Every parameter is a faithful pass-through to the SDK constructor: leave a value
    /// at its default to inherit the SDK's own behaviour rather than imposing one.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="defaultTtl">TTL applied when task creation omits one. <see langword="null"/> (the default) means unlimited.</param>
    /// <param name="maxTtl">Cap applied to a task-requested TTL. <see langword="null"/> (the default) means no maximum.</param>
    /// <param name="pollInterval">Advertised client polling interval. <see langword="null"/> uses the SDK default (one second).</param>
    /// <param name="cleanupInterval">Background expiry-sweep interval. <see langword="null"/> uses the SDK default (one minute); pass <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to disable cleanup.</param>
    /// <param name="pageSize">Maximum tasks returned per <c>tasks/list</c> page. Defaults to 100.</param>
    /// <param name="maxTasks">Global task cap. <see langword="null"/> (the default) means unlimited.</param>
    /// <param name="maxTasksPerSession">Per-session task cap. <see langword="null"/> (the default) means unlimited.</param>
    /// <param name="sendStatusNotifications">When <see langword="true"/>, emits <c>notifications/tasks/status</c> as task status changes (delivered only over stateful HTTP or stdio).</param>
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
        int pageSize = 100,
        int? maxTasks = null,
        int? maxTasksPerSession = null,
        bool sendStatusNotifications = false)
    {
        Guard.NotNull(builder);

        builder.Services.AddSingleton<IMcpTaskStore>(_ => new InMemoryMcpTaskStore(
            defaultTtl, maxTtl, pollInterval, cleanupInterval, pageSize, maxTasks, maxTasksPerSession));

        builder.Services
            .AddOptions<McpServerOptions>()
            .Configure<IMcpTaskStore>((options, store) =>
            {
                options.TaskStore = store;
                options.SendTaskStatusNotifications = sendStatusNotifications;
            });

        return builder;
    }
}
