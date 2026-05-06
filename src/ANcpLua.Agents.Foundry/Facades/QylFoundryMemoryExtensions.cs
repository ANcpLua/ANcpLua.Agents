using ANcpLua.Roslyn.Utilities;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Agents.Foundry;

/// <summary>
///     <c>Qyl</c>-prefixed wrappers around <see cref="FoundryMemoryProvider" /> that
///     bundle the provider ctor, scope construction, and
///     <see cref="FoundryMemoryProvider.EnsureMemoryStoreCreatedAsync" /> into a single
///     async factory.
/// </summary>
public static class QylFoundryMemoryExtensions
{
    /// <summary>
    ///     Builds a <see cref="FoundryMemoryProvider" /> bound to
    ///     <paramref name="memoryStoreName" />, scoped by <paramref name="scope" />,
    ///     and ensures the underlying memory store has been provisioned before returning.
    /// </summary>
    public static async Task<FoundryMemoryProvider> AsQylFoundryMemoryProviderAsync(
        this AIProjectClient client,
        string memoryStoreName,
        string scope,
        string chatModel,
        string embeddingModel,
        string? memoryStoreDescription = null,
        FoundryMemoryProviderOptions? options = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(memoryStoreName);
        Guard.NotNullOrWhiteSpace(scope);
        Guard.NotNullOrWhiteSpace(chatModel);
        Guard.NotNullOrWhiteSpace(embeddingModel);

        FoundryMemoryProviderScope memoryScope = new(scope);
        FoundryMemoryProvider provider = new(
            client,
            memoryStoreName,
            _ => new FoundryMemoryProvider.State(memoryScope),
            options,
            loggerFactory);

        await provider.EnsureMemoryStoreCreatedAsync(
            chatModel,
            embeddingModel,
            memoryStoreDescription,
            cancellationToken).ConfigureAwait(false);

        return provider;
    }

    /// <summary>
    ///     Builds a <see cref="FoundryMemoryProvider" /> with a caller-supplied
    ///     <paramref name="stateInitializer" /> (used for per-session scopes) and
    ///     ensures the memory store exists.
    /// </summary>
    public static async Task<FoundryMemoryProvider> AsQylFoundryMemoryProviderAsync(
        this AIProjectClient client,
        string memoryStoreName,
        Func<AgentSession?, FoundryMemoryProvider.State> stateInitializer,
        string chatModel,
        string embeddingModel,
        string? memoryStoreDescription = null,
        FoundryMemoryProviderOptions? options = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(memoryStoreName);
        Guard.NotNull(stateInitializer);
        Guard.NotNullOrWhiteSpace(chatModel);
        Guard.NotNullOrWhiteSpace(embeddingModel);

        FoundryMemoryProvider provider = new(
            client,
            memoryStoreName,
            stateInitializer,
            options,
            loggerFactory);

        await provider.EnsureMemoryStoreCreatedAsync(
            chatModel,
            embeddingModel,
            memoryStoreDescription,
            cancellationToken).ConfigureAwait(false);

        return provider;
    }
}
