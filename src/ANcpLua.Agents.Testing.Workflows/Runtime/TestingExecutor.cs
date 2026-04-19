// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/TestingExecutor.cs

namespace ANcpLua.Agents.Testing.Workflows;

// Walks a pre-baked list of actions; set loop=true to cycle forever.
// LinkCancellation wires external tokens to the internal CTS so SetCancel()
// cancels every linked source at once. Use it to drive cancellation tests
// deterministically.
internal abstract class TestingExecutor<TIn, TOut> : Executor, IDisposable
{
    private readonly Func<TIn, IWorkflowContext, CancellationToken, ValueTask<TOut>>[] _actions;
    private readonly HashSet<CancellationToken> _linkedTokens = [];
    private readonly bool _loop;
    private CancellationTokenSource _internalCts = new();
    private int _nextActionIndex;

    protected TestingExecutor(string id, bool loop = false,
        params Func<TIn, IWorkflowContext, CancellationToken, ValueTask<TOut>>[] actions)
        : base(id)
    {
        _loop = loop;
        _actions = actions;
    }

    public int Iterations { get; private set; }

    public bool AtEnd => _nextActionIndex >= _actions.Length;

    public bool Completed => !_loop && AtEnd;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void UnlinkCancellation(CancellationToken cancellationToken)
    {
        _linkedTokens.Remove(cancellationToken);
    }

    public void LinkCancellation(CancellationToken cancellationToken)
    {
        _linkedTokens.Add(cancellationToken);
        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource([.. _linkedTokens]);
        tokenSource = Interlocked.Exchange(ref _internalCts, tokenSource);
        tokenSource.Dispose();
    }

    public void SetCancel()
    {
        Volatile.Read(ref _internalCts).Cancel();
    }

    [MessageHandler]
    public ValueTask<TOut> RouteToActionsAsync(TIn message, IWorkflowContext context)
    {
        if (AtEnd)
        {
            if (!_loop) throw new InvalidOperationException("No more actions to execute and looping is disabled.");

            Iterations++;
            _nextActionIndex = 0;
        }

        try
        {
            var action = _actions[_nextActionIndex];
            return action(message, context, Volatile.Read(ref _internalCts).Token);
        }
        finally
        {
            _nextActionIndex++;
        }
    }

    ~TestingExecutor()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        _internalCts.Dispose();
    }
}