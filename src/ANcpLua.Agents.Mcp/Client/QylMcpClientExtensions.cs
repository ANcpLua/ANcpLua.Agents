using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace ANcpLua.Agents.Mcp;

/// <summary>
/// Qyl-prefixed facades over the ModelContextProtocol .NET client.
/// </summary>
public static class QylMcpClientExtensions
{
    /// <summary>
    /// Creates an <see cref="McpClient"/> over an already-constructed <see cref="IClientTransport"/>.
    /// Transport ownership remains with the caller.
    /// </summary>
    /// <param name="transport">The transport to use. Caller owns disposal.</param>
    /// <param name="cancellationToken">A token to cancel the connection.</param>
    /// <returns>The connected MCP client. Dispose to close the session.</returns>
    public static Task<McpClient> CreateQylMcpClientAsync(
        IClientTransport transport,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(transport);

        return McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates a <see cref="QylHttpMcpClient"/> bundle that pairs an <see cref="McpClient"/>
    /// with the underlying <see cref="HttpClientTransport"/> built from the given endpoint.
    /// Disposing the bundle disposes the client and the transport in the correct order.
    /// </summary>
    /// <param name="endpoint">The HTTP endpoint of the MCP server.</param>
    /// <param name="cancellationToken">A token to cancel the connection.</param>
    /// <returns>An ownership bundle. Dispose to release the client and the transport.</returns>
    public static Task<QylHttpMcpClient> CreateQylHttpMcpClientAsync(
        Uri endpoint,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(endpoint);

        return QylHttpMcpClient.ConnectAsync(endpoint, cancellationToken);
    }

    /// <summary>
    /// Lists the server's tools and projects them to <see cref="AITool"/> for use with
    /// Microsoft Agent Framework agents.
    /// </summary>
    /// <param name="client">The connected MCP client.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The server's tools as <see cref="AITool"/>.</returns>
    public static async Task<IList<AITool>> AsAIToolsAsync(
        this McpClient client,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(client);

        IList<McpClientTool> tools = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return [.. tools.Cast<AITool>()];
    }

    /// <summary>
    /// Lists the server's tools and projects them to <see cref="AITool"/> for use with
    /// Microsoft Agent Framework agents.
    /// </summary>
    /// <param name="bundle">The connected MCP client bundle.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The server's tools as <see cref="AITool"/>.</returns>
    public static Task<IList<AITool>> AsAIToolsAsync(
        this QylHttpMcpClient bundle,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(bundle);

        return bundle.Client.AsAIToolsAsync(cancellationToken);
    }
}

