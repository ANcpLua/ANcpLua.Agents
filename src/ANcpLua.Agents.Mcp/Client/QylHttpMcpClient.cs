using ANcpLua.Roslyn.Utilities;
using ModelContextProtocol.Client;

namespace ANcpLua.Agents.Mcp;

/// <summary>
/// Wraps a connected <see cref="McpClient"/> over a <see cref="HttpClientTransport"/>.
/// The SDK's <see cref="McpClient"/> takes ownership of the transport and releases the
/// underlying <c>HttpClient</c> connection pool when disposed, so this bundle disposes
/// only the client — structurally identical to <see cref="QylStdioMcpClient"/>.
/// </summary>
public sealed class QylHttpMcpClient : IAsyncDisposable
{
    private QylHttpMcpClient(McpClient client) => Client = client;

    /// <summary>The connected MCP client. Use this to call <c>ListToolsAsync</c>, etc.</summary>
    public McpClient Client { get; }

    internal static async Task<QylHttpMcpClient> ConnectAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        Guard.NotNull(endpoint);

        HttpClientTransport transport = new(new HttpClientTransportOptions
        {
            Endpoint = endpoint,
            TransportMode = HttpTransportMode.StreamableHttp
        });
        var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new QylHttpMcpClient(client);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Client.DisposeAsync();
}
