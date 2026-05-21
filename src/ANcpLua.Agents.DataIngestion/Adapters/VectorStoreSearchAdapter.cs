using System.Diagnostics;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.VectorData;

namespace ANcpLua.Agents.DataIngestion;

/// <summary>
///     Bridges a writer-produced <see cref="VectorStoreCollection{TKey,TRecord}"/> (with the
///     dynamic <see cref="Dictionary{TKey,TValue}"/> schema emitted by
///     <see cref="Microsoft.Extensions.DataIngestion.VectorStoreWriter{T}"/>) onto the
///     <see cref="TextSearchProvider"/> search-callback contract.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="QylVectorStoreSearchExtensions"/> is the convenience layer for default
///         behavior; this adapter is the configurable, testable form. It exposes field-name
///         overrides, an optional minimum-score filter, a pluggable projection delegate, and
///         per-search OTel spans — knobs you only reach for when the extension defaults don't
///         fit.
///     </para>
///     <para>
///         <see cref="SearchAsync"/> matches the <see cref="TextSearchProvider"/> callback
///         signature directly, so a method-group reference suffices:
///         <c>new TextSearchProvider(adapter.SearchAsync, providerOptions)</c>.
///     </para>
/// </remarks>
public sealed class VectorStoreSearchAdapter
{
    /// <summary>
    ///     Span name and <c>gen_ai.operation.name</c> tag value emitted by
    ///     <see cref="SearchAsync"/> when an <see cref="ActivitySource"/> is supplied.
    /// </summary>
    public const string ActivityName = "rag.search";

    private static readonly VectorStoreSearchAdapterOptions s_defaultOptions = new();

    private readonly VectorStoreCollection<object, Dictionary<string, object?>> _collection;
    private readonly VectorStoreSearchAdapterOptions _options;
    private readonly ActivitySource? _activitySource;

    /// <summary>
    ///     Initializes a new adapter that projects hits from <paramref name="collection"/> into
    ///     <see cref="TextSearchProvider.TextSearchResult"/> instances.
    /// </summary>
    /// <param name="collection">
    ///     The dynamic-schema vector-store collection. Records must carry the field named by
    ///     <see cref="VectorStoreSearchAdapterOptions.ContentField"/> as a non-empty string;
    ///     hits without it are skipped by the default projection.
    /// </param>
    /// <param name="options">
    ///     Behavior overrides. When omitted, the default options surface <c>content</c>,
    ///     <c>summary</c>, and <c>sourcename</c> fields with no score threshold.
    /// </param>
    /// <param name="activitySource">
    ///     Optional <see cref="ActivitySource"/> for per-search OTel spans tagged with
    ///     <c>gen_ai.operation.name = "rag.search"</c>, the query length, the
    ///     <see cref="VectorStoreSearchAdapterOptions.TopResults"/> ceiling, and the scanned /
    ///     kept hit counts.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="collection"/> is <c>null</c>.
    /// </exception>
    public VectorStoreSearchAdapter(
        VectorStoreCollection<object, Dictionary<string, object?>> collection,
        VectorStoreSearchAdapterOptions? options = null,
        ActivitySource? activitySource = null)
    {
        _collection = Guard.NotNull(collection);
        _options = options ?? s_defaultOptions;
        _activitySource = activitySource;
    }

    /// <summary>
    ///     Runs a vector search for <paramref name="query"/> against the wrapped collection and
    ///     projects each surviving hit into a <see cref="TextSearchProvider.TextSearchResult"/>.
    ///     The signature matches <see cref="TextSearchProvider"/>'s callback parameter so the
    ///     method group can be passed directly.
    /// </summary>
    /// <param name="query">The user query to embed and search with.</param>
    /// <param name="cancellationToken">Cancels the underlying vector search.</param>
    /// <returns>
    ///     A list of projected results, ordered by the collection's native ranking and capped at
    ///     <see cref="VectorStoreSearchAdapterOptions.TopResults"/>. Hits filtered out by
    ///     <see cref="VectorStoreSearchAdapterOptions.MinScore"/> or by a projection returning
    ///     <c>null</c> are omitted.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="query"/> is <c>null</c>.
    /// </exception>
    public async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(query);

        using var activity = _activitySource?.StartActivity(ActivityName, ActivityKind.Internal);
        activity?.SetTag("gen_ai.operation.name", ActivityName);
        activity?.SetTag("ancplua.rag.query.length", query.Length);
        activity?.SetTag("ancplua.rag.top_results", _options.TopResults);

        var results = new List<TextSearchProvider.TextSearchResult>(_options.TopResults);
        var scanned = 0;

        try
        {
            await foreach (var hit in _collection
                .SearchAsync(query, top: _options.TopResults, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                scanned++;

                if (_options.MinScore is { } threshold
                    && hit.Score is { } score
                    && score < threshold)
                {
                    continue;
                }

                var projected = _options.Projection is { } project
                    ? project(hit)
                    : DefaultProject(hit, _options);

                if (projected is not null)
                {
                    results.Add(projected);
                }
            }

            activity?.SetTag("ancplua.rag.hits.scanned", scanned);
            activity?.SetTag("ancplua.rag.hits.kept", results.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return results;
        }
        catch when (TagFailure(activity, scanned, results.Count, cancellationToken))
        {
            throw;
        }
    }

    private static bool TagFailure(Activity? activity, int scanned, int kept, CancellationToken cancellationToken)
    {
        if (activity is null) return false;

        activity.SetTag("ancplua.rag.hits.scanned", scanned);
        activity.SetTag("ancplua.rag.hits.kept", kept);
        if (!cancellationToken.IsCancellationRequested)
            activity.SetStatus(ActivityStatusCode.Error);
        return false;
    }

    private static TextSearchProvider.TextSearchResult? DefaultProject(
        VectorSearchResult<Dictionary<string, object?>> hit,
        VectorStoreSearchAdapterOptions options)
    {
        if (!hit.Record.TryGetValue(options.ContentField, out var contentValue)
            || contentValue is not string content
            || string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var summary = hit.Record.TryGetValue(options.SummaryField, out var summaryValue)
            ? summaryValue as string
            : null;

        var sourceName = hit.Record.TryGetValue(options.SourceNameField, out var sourceValue)
            ? sourceValue as string
            : null;

        var text = string.IsNullOrWhiteSpace(summary)
            ? content
            : $"[Summary] {summary}\n\n[Excerpt] {content}";

        return new TextSearchProvider.TextSearchResult
        {
            Text = text,
            SourceName = sourceName ?? options.DefaultSourceName,
            SourceLink = string.Empty,
        };
    }
}

/// <summary>
///     Configuration knobs for <see cref="VectorStoreSearchAdapter"/>. Records are immutable
///     by design — clone via <c>options with { … }</c> to derive variants.
/// </summary>
public sealed record VectorStoreSearchAdapterOptions
{
    /// <summary>
    ///     Maximum number of hits to request from the underlying collection. Defaults to 4 —
    ///     enough to feed a moderate context window without flooding the prompt.
    /// </summary>
    public int TopResults { get; init; } = 4;

    /// <summary>
    ///     Optional inclusive lower bound on <see cref="VectorSearchResult{TRecord}.Score"/>.
    ///     Hits with a score strictly below the threshold are dropped; hits whose score is
    ///     <c>null</c> (collection didn't return one) are always kept. Defaults to no
    ///     threshold — useful when the corpus is sparse and you'd rather show low-confidence
    ///     excerpts than nothing.
    /// </summary>
    public double? MinScore { get; init; }

    /// <summary>
    ///     Field name on each record holding the chunk text. Defaults to <c>"content"</c> —
    ///     the convention used by
    ///     <see cref="Microsoft.Extensions.DataIngestion.VectorStoreWriter{T}"/>.
    /// </summary>
    public string ContentField { get; init; } = "content";

    /// <summary>
    ///     Field name on each record holding an optional summary. Defaults to <c>"summary"</c> —
    ///     the convention used by
    ///     <see cref="Microsoft.Extensions.DataIngestion.SummaryEnricher"/>.
    /// </summary>
    public string SummaryField { get; init; } = "summary";

    /// <summary>
    ///     Field name on each record holding the source identifier. Defaults to
    ///     <c>"sourcename"</c> — the convention used by ingestion-pipeline readers.
    /// </summary>
    public string SourceNameField { get; init; } = "sourcename";

    /// <summary>
    ///     Fallback <see cref="TextSearchProvider.TextSearchResult.SourceName"/> when a record
    ///     has no value in <see cref="SourceNameField"/>. Defaults to
    ///     <c>"vector-store"</c> — domain-neutral so it doesn't leak demo terminology into
    ///     production agent context.
    /// </summary>
    public string DefaultSourceName { get; init; } = "vector-store";

    /// <summary>
    ///     Optional override for the per-hit projection. When set, replaces the default
    ///     content/summary projection entirely; returning <c>null</c> skips the hit. Use this
    ///     to surface additional record fields (e.g. URLs, timestamps, custom labels) into the
    ///     <see cref="TextSearchProvider.TextSearchResult"/>.
    /// </summary>
    public Func<VectorSearchResult<Dictionary<string, object?>>, TextSearchProvider.TextSearchResult?>? Projection { get; init; }
}
