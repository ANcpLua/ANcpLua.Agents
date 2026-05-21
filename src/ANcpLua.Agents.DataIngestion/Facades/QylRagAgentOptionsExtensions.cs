using System.Diagnostics;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.VectorData;

namespace ANcpLua.Agents.DataIngestion;

/// <summary>
///     Convenience extensions on <see cref="ChatClientAgentOptions"/> that wire a
///     <see cref="VectorStoreCollection{TKey,TRecord}"/> as a RAG context provider in one call.
///     Appends to any existing <see cref="ChatClientAgentOptions.AIContextProviders"/> — multiple
///     <c>WithQylRagSearch</c> calls stack additional providers rather than replacing earlier ones.
/// </summary>
public static class QylRagAgentOptionsExtensions
{
    /// <summary>
    ///     Wraps <paramref name="collection"/> as an <see cref="AIContextProvider"/> via
    ///     <see cref="QylVectorStoreSearchExtensions"/> and appends it to
    ///     <see cref="ChatClientAgentOptions.AIContextProviders"/>. Retrieval runs before every
    ///     LLM call (naive RAG); for agentic RAG, register a vector-search
    ///     <see cref="Microsoft.Extensions.AI.AIFunction"/> tool instead.
    /// </summary>
    /// <param name="options">The agent options to extend.</param>
    /// <param name="collection">The vector store collection produced by an ingestion pipeline.</param>
    /// <param name="topResults">Maximum excerpts to inject per turn.</param>
    /// <param name="defaultSourceName">Fallback source-name attached to retrieved excerpts.</param>
    public static ChatClientAgentOptions WithQylRagSearch(
        this ChatClientAgentOptions options,
        VectorStoreCollection<object, Dictionary<string, object?>> collection,
        int topResults = 4,
        string defaultSourceName = "vector-store")
    {
        Guard.NotNull(options);
        Guard.NotNull(collection);

        var provider = collection.AsQylRagContextProvider(topResults, defaultSourceName);
        return options.AppendProvider(provider);
    }

    /// <summary>
    ///     Configurable overload: wraps <paramref name="collection"/> using the supplied
    ///     <see cref="VectorStoreSearchAdapterOptions"/> and appends the resulting provider.
    ///     Use when you need field-name overrides, a score threshold, a custom projection, or
    ///     tracing.
    /// </summary>
    /// <param name="options">The agent options to extend.</param>
    /// <param name="collection">The vector store collection produced by an ingestion pipeline.</param>
    /// <param name="adapterOptions">Adapter behavior overrides.</param>
    /// <param name="activitySource">
    ///     Optional <see cref="ActivitySource"/> for per-search OTel spans.
    /// </param>
    public static ChatClientAgentOptions WithQylRagSearch(
        this ChatClientAgentOptions options,
        VectorStoreCollection<object, Dictionary<string, object?>> collection,
        VectorStoreSearchAdapterOptions adapterOptions,
        ActivitySource? activitySource = null)
    {
        Guard.NotNull(options);
        Guard.NotNull(collection);
        Guard.NotNull(adapterOptions);

        var provider = collection.AsQylRagContextProvider(adapterOptions, providerOptions: null, activitySource);
        return options.AppendProvider(provider);
    }

    private static ChatClientAgentOptions AppendProvider(this ChatClientAgentOptions options, AIContextProvider provider)
    {
        options.AIContextProviders = options.AIContextProviders is { } existing
            ? [.. existing, provider]
            : [provider];
        return options;
    }
}
