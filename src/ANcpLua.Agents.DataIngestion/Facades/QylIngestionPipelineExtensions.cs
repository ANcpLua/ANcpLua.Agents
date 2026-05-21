using System.Runtime.CompilerServices;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;

namespace ANcpLua.Agents.DataIngestion;

/// <summary>
///     One-call builder for the canonical Microsoft.Extensions.DataIngestion RAG pipeline:
///     <c>MarkdownReader → SemanticSimilarityChunker → SummaryEnricher → VectorStoreWriter</c>.
///     Returns a <see cref="QylIngestionPipelineHandle"/> that owns the pipeline + writer
///     lifecycle and exposes the resulting <see cref="VectorStoreCollection{TKey,TRecord}"/> for
///     wire-in via the extensions in <see cref="QylRagAgentOptionsExtensions"/>.
/// </summary>
public static class QylIngestionPipelineExtensions
{
    /// <summary>
    ///     Builds the standard RAG ingestion pipeline. The caller supplies the storage backend
    ///     (<paramref name="vectorStore"/>), the embedder, the enricher's chat client, and a
    ///     tokenizer — the facade picks defaults for chunking topology and wires the standard
    ///     <see cref="SummaryEnricher"/> chunk processor.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The defaults (150-token chunks with 20-token overlap) target retrieval over
    ///         technical documentation; tune them via <paramref name="maxTokensPerChunk"/> and
    ///         <paramref name="overlapTokens"/> for prose-heavy or table-heavy corpora.
    ///     </para>
    ///     <para>
    ///         <paramref name="dimensionCount"/> must match the embedding model the caller's
    ///         <paramref name="embeddings"/> generator produces — 1536 for OpenAI
    ///         <c>text-embedding-3-small</c>, 3072 for <c>text-embedding-3-large</c>, etc.
    ///         Mismatches are caught by the underlying vector store, not by this facade.
    ///     </para>
    /// </remarks>
    /// <param name="vectorStore">The backing vector store (InMemory, SQLite, Qdrant, …).</param>
    /// <param name="embeddings">Embedding generator used by the semantic chunker.</param>
    /// <param name="enricherChatClient">Chat client used by the <see cref="SummaryEnricher"/>.</param>
    /// <param name="tokenizer">Tokenizer used by the chunker — match it to the chat model.</param>
    /// <param name="dimensionCount">Vector dimensionality declared on the collection.</param>
    /// <param name="collectionName">Collection name within the vector store.</param>
    /// <param name="maxTokensPerChunk">Upper bound on tokens per chunk.</param>
    /// <param name="overlapTokens">Number of overlapping tokens between consecutive chunks.</param>
    /// <param name="loggerFactory">Optional logger factory for pipeline + enricher diagnostics.</param>
    public static QylIngestionPipelineHandle BuildQylIngestionPipeline(
        VectorStore vectorStore,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        IChatClient enricherChatClient,
        Tokenizer tokenizer,
        int dimensionCount = 1536,
        string collectionName = "chunks",
        int maxTokensPerChunk = 150,
        int overlapTokens = 20,
        ILoggerFactory? loggerFactory = null)
    {
        Guard.NotNull(vectorStore);
        Guard.NotNull(embeddings);
        Guard.NotNull(enricherChatClient);
        Guard.NotNull(tokenizer);
        Guard.NotNullOrWhiteSpace(collectionName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dimensionCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTokensPerChunk);
        ArgumentOutOfRangeException.ThrowIfNegative(overlapTokens);

        var enricherOptions = new EnricherOptions(enricherChatClient) { LoggerFactory = loggerFactory };
        var chunkerOptions = new IngestionChunkerOptions(tokenizer)
        {
            MaxTokensPerChunk = maxTokensPerChunk,
            OverlapTokens = overlapTokens,
        };

        IngestionChunker<string> chunker = new SemanticSimilarityChunker(embeddings, chunkerOptions);
        IngestionChunkProcessor<string> summary = new SummaryEnricher(enricherOptions);
        IngestionDocumentReader reader = new MarkdownReader();

        VectorStoreWriter<string>? writer = null;
        IngestionPipeline<string>? pipeline = null;
        try
        {
            writer = new VectorStoreWriter<string>(
                vectorStore,
                dimensionCount,
                new VectorStoreWriterOptions { CollectionName = collectionName });

            pipeline = new IngestionPipeline<string>(reader, chunker, writer, loggerFactory: loggerFactory);
            pipeline.ChunkProcessors.Add(summary);

            var handle = new QylIngestionPipelineHandle(pipeline, writer);
            pipeline = null;
            writer = null;
            return handle;
        }
        finally
        {
            pipeline?.Dispose();
            writer?.Dispose();
        }
    }
}

/// <summary>
///     Owns the lifecycle of a <see cref="IngestionPipeline{T}"/> + its <see cref="VectorStoreWriter{T}"/>
///     and surfaces the produced <see cref="VectorStoreCollection{TKey,TRecord}"/> for downstream
///     RAG wire-in. Built by <see cref="QylIngestionPipelineExtensions.BuildQylIngestionPipeline"/>.
/// </summary>
public sealed class QylIngestionPipelineHandle(
    IngestionPipeline<string> pipeline,
    VectorStoreWriter<string> writer) : IAsyncDisposable
{
    /// <summary>The underlying ingestion pipeline; expose only for advanced composition.</summary>
    public IngestionPipeline<string> Pipeline => pipeline;

    /// <summary>The underlying vector-store writer.</summary>
    public VectorStoreWriter<string> Writer => writer;

    /// <summary>
    ///     The collection populated by the pipeline. Pass it to
    ///     <see cref="QylRagAgentOptionsExtensions"/> or
    ///     <see cref="QylVectorStoreSearchExtensions"/> to wire RAG into an agent.
    /// </summary>
    public VectorStoreCollection<object, Dictionary<string, object?>> Collection => writer.VectorStoreCollection;

    /// <summary>
    ///     Runs the pipeline over every file under <paramref name="directory"/> matching
    ///     <paramref name="searchPattern"/>, yielding one <see cref="IngestionResult"/> per file.
    ///     Failures do not abort the stream — inspect each result's
    ///     <see cref="IngestionResult.Succeeded"/> + <see cref="IngestionResult.Exception"/>.
    /// </summary>
    public async IAsyncEnumerable<IngestionResult> IngestDirectoryAsync(
        string directory,
        string searchPattern = "*.md",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(directory);
        Guard.NotNullOrWhiteSpace(searchPattern);

        await foreach (var result in pipeline.ProcessAsync(new DirectoryInfo(directory), searchPattern, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }
    }

    public ValueTask DisposeAsync()
    {
        pipeline.Dispose();
        writer.Dispose();
        return ValueTask.CompletedTask;
    }
}
