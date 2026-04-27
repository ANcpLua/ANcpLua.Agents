// Licensed to the .NET Foundation under one or more agreements.

using System.Collections.Concurrent;
using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Agents.Testing.Conformance.Stores;

/// <summary>
///     Trivial in-memory <see cref="ISessionStateStore{TState}" /> reference implementation.
///     Exists so consumers can inherit <see cref="SessionStateStoreConformanceTests{TStore,TState}" />
///     and run the suite against a known-good baseline before pointing the same suite at their
///     real Mem0 / Cosmos / Foundry-Threads backed store.
/// </summary>
/// <typeparam name="TState">Domain payload type.</typeparam>
public sealed class InMemorySessionStateStore<TState> : ISessionStateStore<TState>
    where TState : class
{
    private readonly ConcurrentDictionary<string, TState> _store = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<TState?> LoadAsync(string sessionId, CancellationToken cancellationToken)
        => Task.FromResult(((IReadOnlyDictionary<string, TState>)_store).GetOrNull(sessionId));

    /// <inheritdoc />
    public Task SaveAsync(string sessionId, TState state, CancellationToken cancellationToken)
    {
        _store[sessionId] = state;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string sessionId, CancellationToken cancellationToken)
    {
        _store.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
