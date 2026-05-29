using ANcpLua.Roslyn.Utilities;
using ModelContextProtocol.Client;

namespace ANcpLua.Agents.Mcp;

/// <summary>
/// Wraps a connected <see cref="McpClient"/> over a <see cref="HttpClientTransport"/>.
/// Once connected, <see cref="McpClient"/> takes ownership of the transport and releases
/// the underlying <c>HttpClient</c> connection pool on dispose, so this bundle disposes
/// only the client. If the connection fails, the transport is disposed explicitly —
/// unlike <see cref="QylStdioMcpClient"/>, whose transport is not disposable,
/// <see cref="HttpClientTransport"/> is <see cref="IAsyncDisposable"/> and owns an
/// <c>HttpClient</c> from construction, so a failed <see cref="McpClient.CreateAsync"/>
/// would otherwise leak it.
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
        try
        {
            return new QylHttpMcpClient(
                await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false));
        }
        catch
        {
            await transport.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Client.DisposeAsync();
}
