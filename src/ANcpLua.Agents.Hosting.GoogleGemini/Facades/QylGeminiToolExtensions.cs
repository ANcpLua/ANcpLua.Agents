using ANcpLua.Roslyn.Utilities;
using Google.GenAI.Types;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Hosting.GoogleGemini;

/// <summary>
///     Named-method facades over Gemini's <see cref="GenerateContentConfig"/> tool surface, which
///     is normally accessed through the per-call <c>RawRepresentationFactory</c> lambda. Each
///     <c>WithQylGemini*</c> extension layers onto any previously-installed factory output rather
///     than replacing it, so multiple calls compose cleanly:
///     <code>
///     options
///         .WithQylGeminiWebSearch()
///         .WithQylGeminiCodeExecution()
///         .WithQylGeminiThinking(ThinkingLevel.High, includeThoughts: true);
///     </code>
/// </summary>
public static class QylGeminiToolExtensions
{
    /// <summary>
    ///     Adds Gemini's <c>GoogleSearch</c> tool with the standard web-search mode. Distinct from
    ///     MEAI's generic <c>HostedWebSearchTool</c>: this path activates Gemini's provider-specific
    ///     grounding behavior. Costs apply per Google's quota — see the Gemini sample for current
    ///     pricing notes.
    /// </summary>
    public static ChatClientAgentOptions WithQylGeminiWebSearch(this ChatClientAgentOptions options)
    {
        Guard.NotNull(options);

        return options.ConfigureGeminiConfig(static config =>
        {
            config.Tools ??= [];
            config.Tools.Add(new Tool
            {
                GoogleSearch = new GoogleSearch
                {
                    SearchTypes = new SearchTypes { WebSearch = new WebSearch() }
                }
            });
        });
    }

    /// <summary>
    ///     Adds Gemini's <c>GoogleMaps</c> tool. When <paramref name="enableWidget"/> is
    ///     <c>true</c>, the response carries a
    ///     <c>GroundingMetadata.GoogleMapsWidgetContextToken</c> that the consumer can pass to
    ///     the Google Maps JavaScript API to render the result on a map.
    /// </summary>
    /// <param name="options">The agent options to extend.</param>
    /// <param name="enableWidget">Whether to populate the Maps widget context token in the response.</param>
    public static ChatClientAgentOptions WithQylGeminiMaps(
        this ChatClientAgentOptions options,
        bool enableWidget = false)
    {
        Guard.NotNull(options);

        return options.ConfigureGeminiConfig(config =>
        {
            config.Tools ??= [];
            config.Tools.Add(new Tool { GoogleMaps = new GoogleMaps { EnableWidget = enableWidget } });
        });
    }

    /// <summary>
    ///     Adds Gemini's <c>CodeExecution</c> tool, which lets the model run Python code in a
    ///     sandboxed server-side environment. The executable code and its output are surfaced on
    ///     the raw <c>GenerateContentResponse</c> via <c>ExecutableCode</c> and
    ///     <c>CodeExecutionResult</c>.
    /// </summary>
    public static ChatClientAgentOptions WithQylGeminiCodeExecution(this ChatClientAgentOptions options)
    {
        Guard.NotNull(options);

        return options.ConfigureGeminiConfig(static config =>
        {
            config.Tools ??= [];
            config.Tools.Add(new Tool { CodeExecution = new ToolCodeExecution() });
        });
    }

    /// <summary>
    ///     Configures Gemini's <c>ThinkingConfig</c>. <see cref="ThinkingLevel"/> replaces the
    ///     older budget-token knob for Gemini 3 and later (Pro supports High/Low; Flash supports
    ///     High/Medium/Low/Minimal). When <paramref name="includeThoughts"/> is <c>true</c>,
    ///     reasoning text is emitted as <see cref="TextReasoningContent"/> on the response.
    /// </summary>
    /// <param name="options">The agent options to extend.</param>
    /// <param name="level">The thinking effort level for Gemini 3+ models.</param>
    /// <param name="includeThoughts">Whether to include reasoning text in the response.</param>
    public static ChatClientAgentOptions WithQylGeminiThinking(
        this ChatClientAgentOptions options,
        ThinkingLevel level,
        bool includeThoughts = false)
    {
        Guard.NotNull(options);

        return options.ConfigureGeminiConfig(config =>
        {
            config.ThinkingConfig = new ThinkingConfig
            {
                ThinkingLevel = level,
                IncludeThoughts = includeThoughts
            };
        });
    }

    private static ChatClientAgentOptions ConfigureGeminiConfig(
        this ChatClientAgentOptions options,
        Action<GenerateContentConfig> mutate)
    {
        options.ChatOptions ??= new ChatOptions();
        var existing = options.ChatOptions.RawRepresentationFactory;
        options.ChatOptions.RawRepresentationFactory = chatClient =>
        {
            var config = existing?.Invoke(chatClient) as GenerateContentConfig ?? new GenerateContentConfig();
            mutate(config);
            return config;
        };
        return options;
    }
}
