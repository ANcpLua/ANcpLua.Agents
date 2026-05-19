using ANcpLua.Agents.DataIngestion;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace ANcpLua.Agents.Tests.DataIngestion;

public sealed class QylRagAgentOptionsExtensionsTests : IDisposable
{
    private readonly InMemoryVectorStore _store = new(new InMemoryVectorStoreOptions
    {
        EmbeddingGenerator = new StubEmbeddingGenerator(),
    });

    public void Dispose() => _store.Dispose();

    [Fact]
    public void WithQylRagSearch_NullOptions_ThrowsArgumentNullException()
    {
        ChatClientAgentOptions options = null!;
        var collection = BuildEmptyCollection();

        Action act = () => options.WithQylRagSearch(collection);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithQylRagSearch_NullCollection_ThrowsArgumentNullException()
    {
        var options = new ChatClientAgentOptions();
        VectorStoreCollection<object, Dictionary<string, object?>> collection = null!;

        Action act = () => options.WithQylRagSearch(collection);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithQylRagSearch_EmptyOptions_AppendsSingleContextProvider()
    {
        var options = new ChatClientAgentOptions();
        var collection = BuildEmptyCollection();

        options.WithQylRagSearch(collection);

        var providers = options.AIContextProviders?.ToArray() ?? [];
        providers.Should().ContainSingle();
        providers[0].Should().BeOfType<TextSearchProvider>();
    }

    [Fact]
    public void WithQylRagSearch_ExistingProviders_AppendsWithoutReplacing()
    {
        var existing = new RecordingContextProvider();
        var options = new ChatClientAgentOptions { AIContextProviders = [existing] };
        var collection = BuildEmptyCollection();

        options.WithQylRagSearch(collection);

        var providers = options.AIContextProviders?.ToArray() ?? [];
        providers.Should().HaveCount(2);
        providers[0].Should().BeSameAs(existing);
        providers[1].Should().BeOfType<TextSearchProvider>();
    }

    [Fact]
    public void WithQylRagSearch_CalledTwice_AppendsTwoDistinctProviders()
    {
        var options = new ChatClientAgentOptions();
        var collection = BuildEmptyCollection();

        options.WithQylRagSearch(collection);
        options.WithQylRagSearch(collection, topResults: 8);

        var providers = options.AIContextProviders?.ToArray() ?? [];
        providers.Should().HaveCount(2);
        providers[0].Should().NotBeSameAs(providers[1]);
    }

    private VectorStoreCollection<object, Dictionary<string, object?>> BuildEmptyCollection() =>
        _store.GetDynamicCollection(
            "test",
            new VectorStoreCollectionDefinition
            {
                Properties =
                [
                    new VectorStoreKeyProperty("key", typeof(string)),
                    new VectorStoreDataProperty("content", typeof(string)),
                    new VectorStoreVectorProperty("embedding", typeof(ReadOnlyMemory<float>), dimensions: 8),
                ],
            });

    private sealed class RecordingContextProvider : AIContextProvider
    {
        protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIContext());
    }

    private sealed class StubEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public EmbeddingGeneratorMetadata Metadata { get; } = new("stub");

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var arr = values.Select(static _ => new Embedding<float>(new float[8])).ToArray();
            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(arr));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
