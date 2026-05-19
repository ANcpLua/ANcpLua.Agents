using ANcpLua.Roslyn.Utilities;
using ModelContextProtocol.Client;

namespace ANcpLua.Agents.Mcp;

/// <summary>
/// An ownership bundle pairing a connected <see cref="McpClient"/> with the
/// <see cref="HttpClientTransport"/> it was created over. Disposing this object disposes
/// the client first (closing the session) and then the transport (releasing the underlying
/// <c>HttpClient</c> connection pool).
/// </summary>
public sealed class QylHttpMcpClient : IAsyncDisposable
{
    private readonly HttpClientTransport _transport;

    private QylHttpMcpClient(McpClient client, HttpClientTransport transport)
    {
        Client = client;
        _transport = transport;
    }

    /// <summary>The connected MCP client. Use this to call <c>ListToolsAsync</c>, etc.</summary>
    public McpClient Client { get; }

    internal static async Task<QylHttpMcpClient> ConnectAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        Guard.NotNull(endpoint);

        HttpClientTransportOptions options = new()
        {
            Endpoint = endpoint,
            TransportMode = HttpTransportMode.StreamableHttp
        };
        HttpClientTransport? transport = null;
        try
        {
            transport = new HttpClientTransport(options);
            McpClient client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
            QylHttpMcpClient bundle = new(client, transport);
            transport = null;
            return bundle;
        }
        finally
        {
            if (transport is not null)
            {
                await transport.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync().ConfigureAwait(false);
        await _transport.DisposeAsync().ConfigureAwait(false);
    }
}
