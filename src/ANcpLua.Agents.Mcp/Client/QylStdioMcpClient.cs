using ANcpLua.Roslyn.Utilities;
using ModelContextProtocol.Client;

namespace ANcpLua.Agents.Mcp;

/// <summary>
/// Wraps a connected <see cref="McpClient"/> over a <see cref="StdioClientTransport"/>.
/// Stdio is the one MCP transport where the server is a child process of the consumer,
/// which is what makes process-boundary observability (RSS / CPU / fd-count) possible
/// per tool call. The MCP SDK's <see cref="McpClient"/> takes ownership of the transport
/// and terminates the child process on dispose, so this bundle does not need a separate
/// transport-disposal step.
/// </summary>
public sealed class QylStdioMcpClient : IAsyncDisposable
{
    private QylStdioMcpClient(McpClient client) => Client = client;

    /// <summary>The connected MCP client. Use this to call <c>ListToolsAsync</c>, <c>ReadResourceAsync</c>, etc.</summary>
    public McpClient Client { get; }

    internal static async Task<QylStdioMcpClient> ConnectAsync(
        StdioClientTransportOptions options,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(options);

        StdioClientTransport transport = new(options);
        var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new QylStdioMcpClient(client);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Client.DisposeAsync();
}
