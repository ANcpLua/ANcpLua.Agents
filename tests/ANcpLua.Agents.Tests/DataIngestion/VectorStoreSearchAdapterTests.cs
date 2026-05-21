using System.Diagnostics;
using ANcpLua.Agents.DataIngestion;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace ANcpLua.Agents.Tests.DataIngestion;

public sealed class VectorStoreSearchAdapterTests : IDisposable
{
    private const int Dimensions = 16;

    private readonly InMemoryVectorStore _store = new(new InMemoryVectorStoreOptions
    {
        EmbeddingGenerator = new HashEmbeddingGenerator(Dimensions),
    });

    public void Dispose() => _store.Dispose();

    [Fact]
    public void Ctor_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        VectorStoreCollection<object, Dictionary<string, object?>> collection = null!;

        // Act
        Action act = () => _ = new VectorStoreSearchAdapter(collection);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var adapter = new VectorStoreSearchAdapter(await BuildCollectionAsync());

        // Act
        Func<Task> act = () => adapter.SearchAsync(null!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchAsync_EmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var adapter = new VectorStoreSearchAdapter(await BuildCollectionAsync());

        // Act
        var results = await adapter.SearchAsync("anything", TestContext.Current.CancellationToken);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ContentOnly_ReturnsRawContentAsText()
    {
        // Arrange
        var collection = await BuildCollectionAsync(new Record("k1", "alpha beta gamma"));
        var adapter = new VectorStoreSearchAdapter(collection);

        // Act
        var results = (await adapter.SearchAsync("alpha beta gamma", TestContext.Current.CancellationToken)).ToArray();

        // Assert
        results.Should().ContainSingle();
        results[0].Text.Should().Be("alpha beta gamma");
        results[0].SourceName.Should().Be("vector-store");
    }

    [Fact]
    public async Task SearchAsync_ContentWithSummary_ProducesSummaryPrefixedText()
    {
        // Arrange
        var collection = await BuildCollectionAsync(new Record("k1", "alpha beta", Summary: "two greek letters", SourceName: "lexicon.md"));
        var adapter = new VectorStoreSearchAdapter(collection);

        // Act
        var results = (await adapter.SearchAsync("alpha beta", TestContext.Current.CancellationToken)).ToArray();

        // Assert
        results.Should().ContainSingle();
        results[0].Text.Should().Be("[Summary] two greek letters\n\n[Excerpt] alpha beta");
        results[0].SourceName.Should().Be("lexicon.md");
    }

    [Fact]
    public async Task SearchAsync_RecordMissingContent_IsSkippedByDefaultProjection()
    {
        // Arrange
        var collection = await BuildCollectionAsync(
            new Record("k1", Content: null, Summary: "orphan summary"),
            new Record("k2", "valid content"));
        var adapter = new VectorStoreSearchAdapter(collection);

        // Act
        var results = (await adapter.SearchAsync("valid content", TestContext.Current.CancellationToken)).ToArray();

        // Assert
        results.Should().ContainSingle();
        results[0].Text.Should().Be("valid content");
    }

    [Fact]
    public async Task SearchAsync_BlankContent_IsSkippedByDefaultProjection()
    {
        // Arrange
        var collection = await BuildCollectionAsync(
            new Record("k1", "   "),
            new Record("k2", "real content"));
        var adapter = new VectorStoreSearchAdapter(collection);

        // Act
        var results = (await adapter.SearchAsync("real content", TestContext.Current.CancellationToken)).ToArray();

        // Assert
        results.Should().ContainSingle();
        results[0].Text.Should().Be("real content");
    }

    [Fact]
    public async Task SearchAsync_MinScoreFilter_DropsLowScoringHits()
    {
        // Arrange — query matches "alpha beta" exactly (score == 1.0); "gamma delta" scores lower.
        var collection = await BuildCollectionAsync(
            new Record("k1", "alpha beta"),
            new Record("k2", "gamma delta"));
        var adapter = new VectorStoreSearchAdapter(collection, new VectorStoreSearchAdapterOptions
        {
            TopResults = 10,
            MinScore = 0.99,
        });

        // Act
        var results = (await adapter.SearchAsync("alpha beta", TestContext.Current.CancellationToken)).ToArray();

        // Assert
        results.Should().ContainSingle();
        results[0].Text.Should().Be("alpha beta");
    }

    [Fact]
    public async Task SearchAsync_CustomProjection_ReplacesDefault()
    {
        // Arrange
        var collection = await BuildCollectionAsync(new Record("k1", "ignored content", Summary: "also ignored"));
        var adapter = new VectorStoreSearchAdapter(collection, new VectorStoreSearchAdapterOptions
        {
            Projection = hit => new TextSearchProvider.TextSearchResult
            {
                Text = "custom-text",
                SourceName = "custom-source",
                SourceLink = $"score={hit.Score:F2}",
            },
        });

        // Act
        var results = (await adapter.SearchAsync("ignored content", TestContext.Current.CancellationToken)).ToArray();

        // Assert
        results.Should().ContainSingle();
        results[0].Text.Should().Be("custom-text");
        results[0].SourceName.Should().Be("custom-source");
        results[0].SourceLink.Should().StartWith("score=");
    }

    [Fact]
    public async Task SearchAsync_CustomProjectionReturningNull_SkipsHit()
    {
        // Arrange
        var collection = await BuildCollectionAsync(
            new Record("keep", "this stays"),
            new Record("drop", "this drops"));
        var adapter = new VectorStoreSearchAdapter(collection, new VectorStoreSearchAdapterOptions
        {
            TopResults = 10,
            Projection = hit =>
            {
                var content = (string)hit.Record["content"]!;
                return content.StartsWith("this drops", StringComparison.Ordinal)
                    ? null
                    : new TextSearchProvider.TextSearchResult { Text = content, SourceName = "x", SourceLink = "" };
            },
        });

        // Act
        var results = (await adapter.SearchAsync("this stays", TestContext.Current.CancellationToken)).ToArray();

        // Assert
        results.Should().ContainSingle(r => r.Text == "this stays");
    }

    [Fact]
    public async Task SearchAsync_CustomFieldNames_AreHonored()
    {
        // Arrange — record stores content under "body" and source under "origin".
        var collection = await BuildCollectionAsync(
            keyField: "key",
            contentField: "body",
            summaryField: "abstract",
            sourceField: "origin",
            new Record("k1", "renamed content", Summary: "renamed summary", SourceName: "renamed-source"));

        var adapter = new VectorStoreSearchAdapter(collection, new VectorStoreSearchAdapterOptions
        {
            ContentField = "body",
            SummaryField = "abstract",
            SourceNameField = "origin",
        });

        // Act
        var results = (await adapter.SearchAsync("renamed content", TestContext.Current.CancellationToken)).ToArray();

        // Assert
        results.Should().ContainSingle();
        results[0].Text.Should().Contain("renamed summary").And.Contain("renamed content");
        results[0].SourceName.Should().Be("renamed-source");
    }

    [Fact]
    public async Task SearchAsync_WithActivitySource_EmitsSpanWithExpectedTags()
    {
        // Arrange
        using var source = new ActivitySource("ANcpLua.Agents.Tests.DataIngestion");
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == source.Name,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);

        var collection = await BuildCollectionAsync(new Record("k1", "trace this content"));
        var adapter = new VectorStoreSearchAdapter(collection, options: null, activitySource: source);

        // Act
        _ = (await adapter.SearchAsync("trace this content", TestContext.Current.CancellationToken)).ToArray();

        // Assert
        activities.Should().ContainSingle();
        var activity = activities[0];
        activity.OperationName.Should().Be(VectorStoreSearchAdapter.ActivityName);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
        activity.GetTagItem("gen_ai.operation.name").Should().Be(VectorStoreSearchAdapter.ActivityName);
        activity.GetTagItem("ancplua.rag.query.length").Should().Be("trace this content".Length);
        activity.GetTagItem("ancplua.rag.hits.kept").Should().Be(1);
    }

    private async Task<VectorStoreCollection<object, Dictionary<string, object?>>> BuildCollectionAsync(params Record[] records)
        => await BuildCollectionAsync(keyField: "key", contentField: "content", summaryField: "summary", sourceField: "sourcename", records);

    private async Task<VectorStoreCollection<object, Dictionary<string, object?>>> BuildCollectionAsync(
        string keyField,
        string contentField,
        string summaryField,
        string sourceField,
        params Record[] records)
    {
        var collection = _store.GetDynamicCollection(
            Guid.NewGuid().ToString("N"),
            new VectorStoreCollectionDefinition
            {
                Properties =
                [
                    new VectorStoreKeyProperty(keyField, typeof(string)),
                    new VectorStoreDataProperty(contentField, typeof(string)),
                    new VectorStoreDataProperty(summaryField, typeof(string)),
                    new VectorStoreDataProperty(sourceField, typeof(string)),
                    new VectorStoreVectorProperty("embedding", typeof(string), dimensions: Dimensions)
                    {
                        EmbeddingType = typeof(Embedding<float>),
                    },
                ],
            });

        await collection.EnsureCollectionExistsAsync(TestContext.Current.CancellationToken);

        foreach (var record in records)
        {
            await collection.UpsertAsync(new Dictionary<string, object?>
            {
                [keyField] = record.Key,
                [contentField] = record.Content,
                [summaryField] = record.Summary,
                [sourceField] = record.SourceName,
                ["embedding"] = record.Content ?? string.Empty,
            }, TestContext.Current.CancellationToken);
        }

        return collection;
    }

    private sealed record Record(string Key, string? Content = null, string? Summary = null, string? SourceName = null);

    private sealed class HashEmbeddingGenerator(int dimensions) : IEmbeddingGenerator<string, Embedding<float>>
    {
        public EmbeddingGeneratorMetadata Metadata { get; } = new("hash");

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var arr = values.Select(v => new Embedding<float>(Embed(v ?? string.Empty, dimensions))).ToArray();
            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(arr));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }

        private static ReadOnlyMemory<float> Embed(string text, int dims)
        {
            var vec = new float[dims];
            for (var i = 0; i < text.Length; i++)
            {
                vec[i % dims] += text[i];
            }

            var norm = 0d;
            for (var i = 0; i < dims; i++)
            {
                norm += vec[i] * vec[i];
            }

            norm = Math.Sqrt(norm);
            if (norm > 0)
            {
                for (var i = 0; i < dims; i++)
                {
                    vec[i] = (float)(vec[i] / norm);
                }
            }

            return vec;
        }
    }
}
