// Licensed to the .NET Foundation under one or more agreements.

using Xunit;

namespace ANcpLua.Agents.Testing.Conformance.Stores;

/// <summary>
///     Provider-agnostic conformance suite for <see cref="ISessionStateStore{TState}" />. Inherit
///     this class with a concrete store + payload type to run a fixed battery of round-trip
///     scenarios — load-after-save identity, idempotent delete, isolated concurrent sessions,
///     overwrite semantics, and absent-load returns null.
/// </summary>
/// <typeparam name="TStore">Concrete store implementation under test.</typeparam>
/// <typeparam name="TState">Domain payload type round-tripped through the store.</typeparam>
public abstract class SessionStateStoreConformanceTests<TStore, TState> : IAsyncLifetime
    where TStore : ISessionStateStore<TState>
    where TState : class
{
    /// <summary>The store under test, built once per test instance via <see cref="CreateStoreAsync" />.</summary>
    protected TStore Store { get; private set; } = default!;

    /// <summary>Override to construct a fresh store per test. Disposed via <see cref="DisposeStoreAsync" />.</summary>
    protected abstract Task<TStore> CreateStoreAsync();

    /// <summary>Override to release any resources owned by the store. Default: no-op.</summary>
    protected virtual Task DisposeStoreAsync() => Task.CompletedTask;

    /// <summary>Override to produce a fresh sample state each call. Two calls must not return the same instance.</summary>
    protected abstract TState SampleState(int seed);

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Store = await CreateStoreAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await DisposeStoreAsync().ConfigureAwait(false);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task LoadAfterSavePreservesPayloadAsync()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sessionId = NewSessionId();
        var saved = SampleState(seed: 1);

        // Act
        await Store.SaveAsync(sessionId, saved, ct).ConfigureAwait(false);
        var loaded = await Store.LoadAsync(sessionId, ct).ConfigureAwait(false);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(saved, loaded);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task LoadOfAbsentSessionReturnsNullAsync()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var loaded = await Store.LoadAsync(NewSessionId(), ct).ConfigureAwait(false);

        // Assert
        Assert.Null(loaded);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task SaveOverwritesExistingStateAsync()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sessionId = NewSessionId();
        var first = SampleState(seed: 10);
        var second = SampleState(seed: 11);

        // Act
        await Store.SaveAsync(sessionId, first, ct).ConfigureAwait(false);
        await Store.SaveAsync(sessionId, second, ct).ConfigureAwait(false);
        var loaded = await Store.LoadAsync(sessionId, ct).ConfigureAwait(false);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(second, loaded);
        Assert.NotEqual(first, loaded);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task DeleteIsIdempotentAsync()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sessionId = NewSessionId();

        // Act / Assert — must not throw on either call
        await Store.DeleteAsync(sessionId, ct).ConfigureAwait(false);
        await Store.DeleteAsync(sessionId, ct).ConfigureAwait(false);

        var loaded = await Store.LoadAsync(sessionId, ct).ConfigureAwait(false);
        Assert.Null(loaded);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task DeleteRemovesPersistedStateAsync()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sessionId = NewSessionId();
        await Store.SaveAsync(sessionId, SampleState(seed: 20), ct).ConfigureAwait(false);

        // Act
        await Store.DeleteAsync(sessionId, ct).ConfigureAwait(false);
        var loaded = await Store.LoadAsync(sessionId, ct).ConfigureAwait(false);

        // Assert
        Assert.Null(loaded);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task ConcurrentSessionsAreIsolatedAsync()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sessionA = NewSessionId();
        var sessionB = NewSessionId();
        var stateA = SampleState(seed: 100);
        var stateB = SampleState(seed: 200);

        // Act — two parallel save+load round-trips through the same store
        var taskA = Task.Run(async () =>
        {
            await Store.SaveAsync(sessionA, stateA, ct).ConfigureAwait(false);
            return await Store.LoadAsync(sessionA, ct).ConfigureAwait(false);
        }, ct);

        var taskB = Task.Run(async () =>
        {
            await Store.SaveAsync(sessionB, stateB, ct).ConfigureAwait(false);
            return await Store.LoadAsync(sessionB, ct).ConfigureAwait(false);
        }, ct);

        var loadedA = await taskA.ConfigureAwait(false);
        var loadedB = await taskB.ConfigureAwait(false);

        // Assert
        Assert.NotNull(loadedA);
        Assert.NotNull(loadedB);
        Assert.Equal(stateA, loadedA);
        Assert.Equal(stateB, loadedB);
    }

    private static string NewSessionId() => $"sess-{Guid.NewGuid():N}";
}
