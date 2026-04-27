// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Agents.Testing.Conformance.Stores;

/// <summary>
///     Generic provider contract for any session-keyed state store. Concrete consumer stores
///     (qyl <c>LoomRunStateStore</c>, Mem0, Cosmos NoSQL, Foundry threads, in-memory) implement
///     this interface and inherit <see cref="SessionStateStoreConformanceTests{TStore,TState}" />
///     to verify uniform round-trip semantics across backends.
/// </summary>
/// <typeparam name="TState">
///     Domain payload type. Constrained to reference types so an absent entry can be reported
///     as <c>null</c> from <see cref="LoadAsync" /> without ambiguity. Records, classes, and
///     immutable collections are the typical shapes; value-type payloads should be wrapped.
/// </typeparam>
public interface ISessionStateStore<TState> where TState : class
{
    /// <summary>Load the state for <paramref name="sessionId" />, or <c>null</c> if absent.</summary>
    Task<TState?> LoadAsync(string sessionId, CancellationToken cancellationToken);

    /// <summary>Persist <paramref name="state" /> under <paramref name="sessionId" />.</summary>
    Task SaveAsync(string sessionId, TState state, CancellationToken cancellationToken);

    /// <summary>
    ///     Delete <paramref name="sessionId" />'s state. Idempotent — deleting a non-existent
    ///     session must not throw.
    /// </summary>
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken);
}
