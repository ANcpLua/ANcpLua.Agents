using System.Diagnostics;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.VectorData;

namespace ANcpLua.Agents.DataIngestion;

/// <summary>
///     One-call facades over <see cref="VectorStoreSearchAdapter"/> that wrap a writer-produced
///     <see cref="VectorStoreCollection{TKey,TRecord}"/> as an <see cref="AIContextProvider"/>.
///     Reach for the adapter directly when you need to vary field names, set a score threshold,
///     plug a custom projection, or emit tracing spans.
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
    /// <param name="providerOptions">
    ///     Optional <see cref="TextSearchProviderOptions"/>. Defaults to
    ///     <see cref="TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke"/> — i.e. naive
    ///     RAG, not agentic. For agentic RAG, register a vector-search <c>AIFunction</c> tool
    ///     instead of using this provider.
    /// </param>
    public static AIContextProvider AsQylRagContextProvider(
        this VectorStoreCollection<object, Dictionary<string, object?>> collection,
        int topResults = 4,
        string defaultSourceName = "vector-store",
        TextSearchProviderOptions? providerOptions = null)
    {
        Guard.NotNull(collection);
        Guard.NotNullOrWhiteSpace(defaultSourceName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topResults);

        var adapterOptions = new VectorStoreSearchAdapterOptions
        {
            TopResults = topResults,
            DefaultSourceName = defaultSourceName,
        };

        return collection.AsQylRagContextProvider(adapterOptions, providerOptions);
    }

    /// <summary>
    ///     Configurable overload: wraps <paramref name="collection"/> as an
    ///     <see cref="AIContextProvider"/> using the supplied
    ///     <see cref="VectorStoreSearchAdapterOptions"/>. Use when you need field-name overrides,
    ///     a score threshold, a custom projection, or tracing.
    /// </summary>
    /// <param name="collection">The writer-produced collection.</param>
    /// <param name="adapterOptions">Adapter behavior overrides.</param>
    /// <param name="providerOptions">
    ///     Optional <see cref="TextSearchProviderOptions"/>. Defaults to
    ///     <see cref="TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke"/>.
    /// </param>
    /// <param name="activitySource">
    ///     Optional <see cref="ActivitySource"/> for per-search OTel spans. See
    ///     <see cref="VectorStoreSearchAdapter.ActivityName"/>.
    /// </param>
    public static AIContextProvider AsQylRagContextProvider(
        this VectorStoreCollection<object, Dictionary<string, object?>> collection,
        VectorStoreSearchAdapterOptions adapterOptions,
        TextSearchProviderOptions? providerOptions = null,
        ActivitySource? activitySource = null)
    {
        Guard.NotNull(collection);
        Guard.NotNull(adapterOptions);

        var adapter = new VectorStoreSearchAdapter(collection, adapterOptions, activitySource);

        var searchOptions = providerOptions ?? new TextSearchProviderOptions
        {
            SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
        };

        return new TextSearchProvider(adapter.SearchAsync, searchOptions);
    }
}
