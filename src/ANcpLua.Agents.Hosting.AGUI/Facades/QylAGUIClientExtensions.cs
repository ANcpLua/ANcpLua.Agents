using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Hosting.AGUI;

/// <summary>
/// Qyl-prefixed adapters that turn an <see cref="HttpClient"/> pointed at an AG-UI server
/// into an <see cref="IChatClient"/> or <see cref="AIAgent"/>.
/// </summary>
public static class QylAGUIClientExtensions
{
    /// <summary>
    /// Returns an <see cref="IChatClient"/> that talks AG-UI over <paramref name="httpClient"/> to <paramref name="baseUri"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for the streaming connection.</param>
    /// <param name="baseUri">Absolute URL of the AG-UI endpoint (for example, <c>http://localhost:5000</c>).</param>
    public static IChatClient AsQylAGUIChatClient(this HttpClient httpClient, Uri baseUri)
    {
        Guard.NotNull(httpClient);
        Guard.NotNull(baseUri);

        return new AGUIChatClient(httpClient, baseUri.ToString());
    }

    /// <summary>
    /// Returns an <see cref="AIAgent"/> backed by an AG-UI chat client, optionally with client-side <paramref name="tools"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for the streaming connection.</param>
    /// <param name="baseUri">Absolute URL of the AG-UI endpoint.</param>
    /// <param name="tools">Optional client-side tools the agent can invoke locally.</param>
    public static AIAgent AsQylAGUIAgent(
        this HttpClient httpClient,
        Uri baseUri,
        IList<AITool>? tools = null)
    {
        Guard.NotNull(httpClient);
        Guard.NotNull(baseUri);

        return new AGUIChatClient(httpClient, baseUri.ToString()).AsAIAgent(tools: tools);
    }
}
