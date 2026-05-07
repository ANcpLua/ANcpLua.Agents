using System.ComponentModel;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace ManagedAgentTelemetry.Host.Integrations;

/// <summary>
/// RAG tool exposed to the agent. The agent decides at runtime whether
/// it needs to consult internal runbooks before producing a recommendation.
///
/// Backed by Azure AI Search — which is the most expensive component of
/// this stack and the prime refactor target for the cost-down exercise.
/// </summary>
public sealed class RunbookSearch(SearchClient search)
{
    [Description("Search internal operations runbooks for procedures, troubleshooting steps, or post-incident reports matching the query. Returns up to 5 hits with title, excerpt, and a deep link.")]
    public async Task<RunbookHit[]> SearchAsync(
        [Description("A free-text query in the natural language of the operations team.")]
        string query,
        CancellationToken cancellationToken = default)
    {
        SearchOptions options = new()
        {
            Size = 5,
            QueryType = SearchQueryType.Semantic,
            HighlightFields = { "content" }
        };

        Azure.Response<SearchResults<RunbookDocument>> response =
            await search.SearchAsync<RunbookDocument>(query, options, cancellationToken)
                .ConfigureAwait(false);

        List<RunbookHit> hits = [];
        await foreach (SearchResult<RunbookDocument> result in response.Value.GetResultsAsync().ConfigureAwait(false))
        {
            hits.Add(new RunbookHit(
                Title: result.Document.Title,
                Excerpt: result.Document.Content[..Math.Min(280, result.Document.Content.Length)],
                Url: result.Document.Url));
        }
        return [.. hits];
    }
}

public sealed record RunbookDocument
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required string Url { get; init; }
}

public sealed record RunbookHit(string Title, string Excerpt, string Url);
