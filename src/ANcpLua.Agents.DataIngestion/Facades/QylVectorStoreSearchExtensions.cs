using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.VectorData;

namespace ANcpLua.Agents.DataIngestion;

/// <summary>
///     Bridge between <see cref="Microsoft.Extensions.DataIngestion.VectorStoreWriter{T}"/>'s
///     produced <see cref="VectorStoreCollection{TKey,TRecord}"/> and
///     <see cref="TextSearchProvider"/>'s search-callback delegate. The writer emits records as
///     <see cref="Dictionary{TKey,TValue}"/> with at minimum a <c>"content"</c> field and,
///     when <see cref="Microsoft.Extensions.DataIngestion.SummaryEnricher"/> is attached, a
///     <c>"summary"</c> field. This extension wires those into a
///     <see cref="TextSearchProvider.TextSearchResult"/> sequence so an agent can consume the
///     retrieval output without a hand-written adapter.
/// </summary>
public static class QylVectorStoreSearchExtensions
{
    /// <summary>
    ///     Wraps <paramref name="collection"/> as an <see cref="AIContextProvider"/> that runs a
    ///     vector search against the collection before each agent invocation and injects up to
    ///     <paramref name="topResults"/> retrieved excerpts into the agent's context.
    /// </summary>
    /// <param name="collection">
    ///     The writer-produced collection. Records must carry at least a string-valued
    ///     <c>"content"</c> field; <c>"summary"</c> and <c>"sourcename"</c> are surfaced when
    ///     present.
    /// </param>
    /// <param name="topResults">Maximum number of excerpts to inject per turn.</param>
    /// <param name="defaultSourceName">
    ///     Fallback <see cref="TextSearchProvider.TextSearchResult.SourceName"/> when a record
    ///     does not carry a <c>"sourcename"</c> field.
    /// </param>
    /// <param name="options">
    ///     Optional behavior overrides. Defaults to
    ///     <see cref="TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke"/> — i.e. naive
    ///     RAG, not agentic. For agentic RAG, register a vector-search <c>AIFunction</c> tool
    ///     instead of using this provider.
    /// </param>
    public static AIContextProvider AsQylRagContextProvider(
        this VectorStoreCollection<object, Dictionary<string, object?>> collection,
        int topResults = 4,
        string defaultSourceName = "manual",
        TextSearchProviderOptions? options = null)
    {
        Guard.NotNull(collection);
        Guard.NotNullOrWhiteSpace(defaultSourceName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topResults);

        var searchOptions = options ?? new TextSearchProviderOptions
        {
            SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
        };

        return new TextSearchProvider(
            BuildSearchCallback(collection, topResults, defaultSourceName),
            searchOptions);
    }

    /// <summary>
    ///     Returns the raw search-callback delegate without wrapping it in a
    ///     <see cref="TextSearchProvider"/>. Useful when composing the callback with custom
    ///     <see cref="TextSearchProviderOptions"/> or stacking it behind a caller-owned provider.
    /// </summary>
    public static Func<string, CancellationToken, Task<IEnumerable<TextSearchProvider.TextSearchResult>>>
        AsQylSearchCallback(
            this VectorStoreCollection<object, Dictionary<string, object?>> collection,
            int topResults = 4,
            string defaultSourceName = "manual")
    {
        Guard.NotNull(collection);
        Guard.NotNullOrWhiteSpace(defaultSourceName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topResults);

        return BuildSearchCallback(collection, topResults, defaultSourceName);
    }

    private static Func<string, CancellationToken, Task<IEnumerable<TextSearchProvider.TextSearchResult>>>
        BuildSearchCallback(
            VectorStoreCollection<object, Dictionary<string, object?>> collection,
            int topResults,
            string defaultSourceName) =>
        async (query, cancellationToken) =>
        {
            var results = new List<TextSearchProvider.TextSearchResult>(topResults);

            await foreach (var hit in collection.SearchAsync(query, top: topResults, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (!hit.Record.TryGetValue("content", out var contentValue)
                    || contentValue is not string content
                    || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                var summary = hit.Record.TryGetValue("summary", out var summaryValue)
                    ? summaryValue as string
                    : null;

                var sourceName = hit.Record.TryGetValue("sourcename", out var sourceValue)
                    ? sourceValue as string
                    : null;

                var text = string.IsNullOrWhiteSpace(summary)
                    ? content
                    : $"[Summary] {summary}\n\n[Excerpt] {content}";

                results.Add(new TextSearchProvider.TextSearchResult
                {
                    Text = text,
                    SourceName = sourceName ?? defaultSourceName,
                    SourceLink = hit.Score is { } score ? $"score={score:F3}" : "",
                });
            }

            return results;
        };
}
