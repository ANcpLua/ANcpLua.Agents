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
    ///     <see cref="QylVectorStoreSearchExtensions.AsQylRagContextProvider"/> and appends it to
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
        string defaultSourceName = "manual")
    {
        Guard.NotNull(options);
        Guard.NotNull(collection);

        var provider = collection.AsQylRagContextProvider(topResults, defaultSourceName);

        options.AIContextProviders = options.AIContextProviders is { } existing
            ? [.. existing, provider]
            : [provider];

        return options;
    }
}
