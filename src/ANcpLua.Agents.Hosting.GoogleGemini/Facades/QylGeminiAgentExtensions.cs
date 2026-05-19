using ANcpLua.Roslyn.Utilities;
using Google.GenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Hosting.GoogleGemini;

/// <summary>
///     Qyl-prefixed facades that build a <see cref="ChatClientAgent"/> on top of a
///     <see cref="Client">Google.GenAI Client</see>. Mirrors the
///     <c>client.AsIChatClient(model)</c> → <c>new ChatClientAgent(...)</c> pattern from the
///     official Google.GenAI samples so consumers only have to remember one entry point.
/// </summary>
public static class QylGeminiAgentExtensions
{
    /// <summary>
    ///     Creates a <see cref="ChatClientAgent"/> over the given Gemini model with no extra
    ///     options. Equivalent to <c>new ChatClientAgent(client.AsIChatClient(model))</c>.
    /// </summary>
    /// <param name="client">The Google.GenAI client.</param>
    /// <param name="model">The Gemini model id (e.g. <c>gemini-3-pro-preview</c>, <c>gemini-3-flash-preview</c>).</param>
    public static ChatClientAgent AsQylGeminiAgent(this Client client, string model)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(model);

        return new ChatClientAgent(client.AsIChatClient(model));
    }

    /// <summary>
    ///     Creates a <see cref="ChatClientAgent"/> over the given Gemini model with the supplied
    ///     <paramref name="options"/>. Compose Gemini-specific options via the
    ///     <c>WithQylGemini*</c> extensions in <see cref="QylGeminiToolExtensions"/> before
    ///     calling this overload.
    /// </summary>
    /// <param name="client">The Google.GenAI client.</param>
    /// <param name="model">The Gemini model id.</param>
    /// <param name="options">Agent construction options — typically built via the <c>WithQylGemini*</c> extensions.</param>
    public static ChatClientAgent AsQylGeminiAgent(
        this Client client,
        string model,
        ChatClientAgentOptions options)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(model);
        Guard.NotNull(options);

        return new ChatClientAgent(client.AsIChatClient(model), options);
    }
}
