using System.Collections.Concurrent;

namespace ANcpLua.Agents.Governance;

/// <summary>
///     Disposable slot returned by <see cref="AgentConcurrencyLimiter.AcquireAsync(string, CancellationToken)"/>.
///     Releases the underlying semaphore permit on dispose, even if the caller throws.
/// </summary>
public sealed class AgentConcurrencySlot : IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private bool _released;

    internal AgentConcurrencySlot(SemaphoreSlim semaphore) => _semaphore = semaphore;

    public ValueTask DisposeAsync()
    {
        if (_released)
            return ValueTask.CompletedTask;

        _released = true;
        _semaphore.Release();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
///     Per-tool concurrency limiter backed by <see cref="SemaphoreSlim"/>. Each tool gets its
///     own semaphore whose initial count is either <see cref="AgentToolPolicy.MaxToolCalls"/>
///     or the default supplied at construction.
/// </summary>
public sealed class AgentConcurrencyLimiter : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new(StringComparer.Ordinal);
    private readonly int _defaultLimit;
    private bool _disposed;

    public AgentConcurrencyLimiter(int defaultLimit = 5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(defaultLimit, 1);
        _defaultLimit = defaultLimit;
    }

    /// <summary>Acquire a slot at the default limit. Blocks asynchronously when full.</summary>
    public async ValueTask<AgentConcurrencySlot> AcquireAsync(
        string toolName, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var semaphore = _semaphores.GetOrAdd(toolName, _ => new SemaphoreSlim(_defaultLimit, _defaultLimit));
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new AgentConcurrencySlot(semaphore);
    }

    /// <summary>
    ///     Acquire a slot using <see cref="AgentToolPolicy.MaxToolCalls"/> as the limit when
    ///     positive; otherwise falls back to the default.
    /// </summary>
    public async ValueTask<AgentConcurrencySlot> AcquireAsync(
        string toolName, AgentToolPolicy policy, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(policy);

        var limit = policy.MaxToolCalls > 0 ? policy.MaxToolCalls : _defaultLimit;
        var semaphore = _semaphores.GetOrAdd(toolName, _ => new SemaphoreSlim(limit, limit));
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new AgentConcurrencySlot(semaphore);
    }

    /// <summary>Available (unacquired) slots for a tool; default when never acquired.</summary>
    public int GetAvailableSlots(string toolName) =>
        _semaphores.TryGetValue(toolName, out var semaphore)
            ? semaphore.CurrentCount
            : _defaultLimit;

    /// <summary>Slots currently held for a tool; 0 when never acquired.</summary>
    public int GetInUseCount(string toolName)
    {
        if (!_semaphores.TryGetValue(toolName, out var semaphore))
            return 0;

        return Math.Max(0, _defaultLimit - semaphore.CurrentCount);
    }

    /// <summary>Disposes the per-tool semaphore so the next acquire creates a fresh one.</summary>
    public void Reset(string toolName)
    {
        if (_semaphores.TryRemove(toolName, out var semaphore))
            semaphore.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var kvp in _semaphores)
            kvp.Value.Dispose();

        _semaphores.Clear();
    }
}
