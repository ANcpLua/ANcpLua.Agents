# ANcpLua.Agents.DataIngestion

Consumer toolkit for Microsoft Agent Framework. RAG facades closing the gap
between `Microsoft.Extensions.DataIngestion` (which produces a
`VectorStoreCollection`) and `Microsoft.Agents.AI.TextSearchProvider` (which
consumes a search-callback delegate), so a `ChatClientAgent` becomes
RAG-capable in one extension method.

- Compatible with: Microsoft.Agents.AI 1.8.x
- Tested against: Microsoft.Agents.AI 1.8.0

## Why

The two sides of RAG ship as orthogonal building blocks today:

- `Microsoft.Extensions.DataIngestion` gives you `IngestionPipeline` →
  `SemanticSimilarityChunker` → `SummaryEnricher` → `VectorStoreWriter`,
  ending at a `VectorStoreCollection<object, Dictionary<string, object?>>`.
- `Microsoft.Agents.AI` gives you `TextSearchProvider`, which expects a
  `Func<string, CancellationToken, Task<IEnumerable<TextSearchResult>>>` and
  wires retrieved excerpts into the agent's context before each LLM call.

Joining them requires an adapter that reads `Dictionary<string, object?>`
records, surfaces the `"content"` and `"summary"` fields, and projects scores.
That adapter is `VectorStoreSearchAdapter`; `QylVectorStoreSearchExtensions`
and `QylRagAgentOptionsExtensions` are the one-call facades. The rest of this
package is small sugar around the same pipeline you would write by hand.

## Surface

```csharp
using ANcpLua.Agents.DataIngestion;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Connectors.InMemory;

await using var ingestion = QylIngestionPipelineExtensions
    .BuildQylIngestionPipeline(
        vectorStore: new InMemoryVectorStore(new() { EmbeddingGenerator = embeddings }),
        embeddings: embeddings,
        enricherChatClient: chatClient,
        tokenizer: TiktokenTokenizer.CreateForModel("gpt-4o-mini"),
        dimensionCount: 1536,
        collectionName: "manual");

await foreach (var result in ingestion.IngestDirectoryAsync("./Data"))
{
    if (!result.Succeeded) Console.WriteLine($"failed: {result.DocumentId}");
}

var agent = chatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        Name = "ManualBot",
        Instructions = "Answer using retrieved excerpts only."
    }
    .WithQylRagSearch(ingestion.Collection));
```

## Configurable form

For score thresholds, custom field names, custom projections, or OTel spans
on each retrieval, drive the adapter directly:

```csharp
using System.Diagnostics;
using ANcpLua.Agents.DataIngestion;

var adapterOptions = new VectorStoreSearchAdapterOptions
{
    TopResults = 6,
    MinScore = 0.65,
    ContentField = "body",
    SourceNameField = "origin",
    DefaultSourceName = "kb",
};

var traceSource = new ActivitySource("MyApp.Rag");

var agent = chatClient.AsAIAgent(
    new ChatClientAgentOptions { Name = "ManualBot" }
        .WithQylRagSearch(ingestion.Collection, adapterOptions, traceSource));
```

The adapter emits an `rag.search` span per retrieval, tagged with
`gen_ai.operation.name`, query length, scanned-hit count, and kept-hit count.

## What the package does *not* do

- Persistence — bring your own `VectorStore` (SQLite, Postgres, Qdrant, …);
  the facade is store-agnostic.
- Embedding provider — pass any `IEmbeddingGenerator<string, Embedding<float>>`.
- Tokenizer data — pass the `Tokenizer` you want; the data package
  (`Microsoft.ML.Tokenizers.Data.O200kBase`, `Cl100kBase`, …) is the caller's
  choice.
- Agentic RAG — `WithQylRagSearch` registers retrieval at
  `BeforeAIInvoke`. For tool-call-driven retrieval, register a vector-search
  `AIFunction` instead and let the agent invoke it on demand.

## License

MIT. See the repository [LICENSE](https://github.com/ANcpLua/ANcpLua.Agents/blob/main/LICENSE).
