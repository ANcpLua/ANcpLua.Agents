using System.Collections.Concurrent;
using ANcpLua.Roslyn.Utilities;

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
    private readonly ConcurrentDictionary<string, SizedSemaphore> _semaphores = new(StringComparer.Ordinal);
    private readonly int _defaultLimit;
    private bool _disposed;

    public AgentConcurrencyLimiter(int defaultLimit = 5)
    {
        Guard.NotLessThan(defaultLimit, 1);
        _defaultLimit = defaultLimit;
    }

    /// <summary>Acquire a slot at the default limit. Blocks asynchronously when full.</summary>
    public async ValueTask<AgentConcurrencySlot> AcquireAsync(
        string toolName, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Guard.NotNullOrWhiteSpace(toolName);

        var sized = _semaphores.GetOrAdd(
            toolName,
            _ => new SizedSemaphore(new SemaphoreSlim(_defaultLimit, _defaultLimit), _defaultLimit));
        await sized.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new AgentConcurrencySlot(sized.Semaphore);
    }

    /// <summary>
    ///     Acquire a slot using <see cref="AgentToolPolicy.MaxToolCalls"/> as the limit when
    ///     positive; otherwise falls back to the default.
    /// </summary>
    public async ValueTask<AgentConcurrencySlot> AcquireAsync(
        string toolName, AgentToolPolicy policy, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Guard.NotNullOrWhiteSpace(toolName);
        Guard.NotNull(policy);

        var limit = policy.MaxToolCalls > 0 ? policy.MaxToolCalls : _defaultLimit;
        var sized = _semaphores.GetOrAdd(
            toolName,
            _ => new SizedSemaphore(new SemaphoreSlim(limit, limit), limit));
        await sized.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new AgentConcurrencySlot(sized.Semaphore);
    }

    /// <summary>Available (unacquired) slots for a tool; default when never acquired.</summary>
    public int GetAvailableSlots(string toolName) =>
        _semaphores.TryGetValue(toolName, out var sized)
            ? sized.Semaphore.CurrentCount
            : _defaultLimit;

    /// <summary>Slots currently held for a tool; 0 when never acquired.</summary>
    public int GetInUseCount(string toolName)
    {
        if (!_semaphores.TryGetValue(toolName, out var sized))
            return 0;

        return Math.Max(0, sized.InitialSize - sized.Semaphore.CurrentCount);
    }

    /// <summary>Disposes the per-tool semaphore so the next acquire creates a fresh one.</summary>
    public void Reset(string toolName)
    {
        if (_semaphores.TryRemove(toolName, out var sized))
            sized.Semaphore.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var kvp in _semaphores)
            kvp.Value.Semaphore.Dispose();

        _semaphores.Clear();
    }

    private sealed record SizedSemaphore(SemaphoreSlim Semaphore, int InitialSize);
}
