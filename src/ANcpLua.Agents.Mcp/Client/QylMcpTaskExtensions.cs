using System.Diagnostics;
using System.Text.Json;
using ANcpLua.Roslyn.Utilities;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ANcpLua.Agents.Mcp;

/// <summary>
/// Long-running MCP tool calls with built-in OTel correlation. Wraps the SDK's three-step
/// task lifecycle (<see cref="McpClient.CallToolAsTaskAsync"/> → <c>PollTaskUntilCompleteAsync</c>
/// → <c>GetTaskResultAsync</c>) under a single span so progress notifications become
/// mid-flight <see cref="ActivityEvent"/>s instead of one fat span at the end.
/// </summary>
[Experimental("MCPEXP001")]
public static class QylMcpTaskExtensions
{
    /// <summary>
    ///     Runs an MCP tool as a long-running task, emitting an OTel span that covers the
    ///     full lifecycle and adds one <see cref="ActivityEvent"/> per progress notification.
    /// </summary>
    /// <param name="client">The connected MCP client.</param>
    /// <param name="toolName">The MCP tool to invoke.</param>
    /// <param name="arguments">Arguments forwarded to the tool. May be <c>null</c>.</param>
    /// <param name="source">ActivitySource that emits the lifecycle span.</param>
    /// <param name="observer">Optional caller-side progress observer invoked alongside the span events.</param>
    /// <param name="cancellationToken">Propagates cancellation through the full lifecycle (start, poll, fetch).</param>
    /// <returns>The task's final result payload, as returned by <c>GetTaskResultAsync</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         The span is named <c>mcp.task &lt;toolName&gt;</c> and carries the following tags
    ///         (best-effort — MCP does not yet have a standard semconv mapping):
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description><c>mcp.tool.name</c> — the invoked tool</description></item>
    ///         <item><description><c>mcp.task.id</c> — set once <c>CallToolAsTaskAsync</c> returns</description></item>
    ///         <item><description><c>mcp.task.status</c> — final status after polling completes</description></item>
    ///     </list>
    ///     <para>
    ///         Each progress notification adds an <c>mcp.task.progress</c> event with
    ///         <c>progress</c>, <c>total</c>, and <c>message</c> tags so the timeline reflects
    ///         server-reported state. The caller's <paramref name="observer"/> receives the same
    ///         values verbatim (independent of OTel emission).
    ///     </para>
    /// </remarks>
    public static async Task<JsonElement> RunQylToolAsTaskAsync(
        this McpClient client,
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments,
        ActivitySource source,
        IProgress<ProgressNotificationValue>? observer = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(toolName);
        Guard.NotNull(source);

        using var activity = source.StartActivity($"mcp.task {toolName}", ActivityKind.Client);
        activity?.SetTag("mcp.tool.name", toolName);

        var progressBridge = new Progress<ProgressNotificationValue>(value =>
        {
            if (activity is not null)
            {
                ActivityTagsCollection tags = new()
                {
                    { "progress", value.Progress },
                    { "total", value.Total },
                    { "message", value.Message },
                };
                activity.AddEvent(new ActivityEvent("mcp.task.progress", tags: tags));
            }

            observer?.Report(value);
        });

        try
        {
            var task = await client.CallToolAsTaskAsync(
                toolName,
                arguments,
                progress: progressBridge,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            activity?.SetTag("mcp.task.id", task.TaskId);

            var completed = await client.PollTaskUntilCompleteAsync(
                task.TaskId,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            activity?.SetTag("mcp.task.status", completed.Status.ToString());

            var result = await client.GetTaskResultAsync(
                task.TaskId,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Cancelled");
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
