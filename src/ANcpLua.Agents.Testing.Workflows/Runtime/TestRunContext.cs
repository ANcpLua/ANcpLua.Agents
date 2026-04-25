// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/TestRunContext.cs

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace ANcpLua.Agents.Testing.Workflows;

/// <summary>
///     Hand-rolled <see cref="IRunnerContext" /> for unit tests that isolate edge
///     runners, request ports, or executor protocol plumbing from the real
///     <c>InProcessRunnerContext</c>. Collects everything it sees into plain lists
///     and dictionaries that tests can assert against.
/// </summary>
public class TestRunContext : IRunnerContext
{
    public Collection<WorkflowEvent> Events { get; } = [];

    public ConcurrentQueue<ExternalRequest> ExternalRequests { get; } = [];

    public Dictionary<string, Executor> Executors { get; set; } = [];

    public string StartingExecutorId { get; set; } = string.Empty;

    internal Dictionary<string, List<MessageEnvelope>> QueuedMessages { get; } = [];

    internal Dictionary<string, List<object>> QueuedOutputs { get; } = [];

    public bool IsCheckpointingEnabled => false;

    public bool ConcurrentRunsEnabled => false;

    WorkflowTelemetryContext IRunnerContext.TelemetryContext => WorkflowTelemetryContext.Disabled;

    public ValueTask AddEventAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken)
    {
        Events.Add(workflowEvent);
        return default;
    }

    public IWorkflowContext BindWorkflowContext(string executorId, Dictionary<string, string>? traceContext = null)
    {
        return new BoundContext(executorId, this, traceContext);
    }

    public ValueTask PostAsync(ExternalRequest request)
    {
        ExternalRequests.Enqueue(request);
        return default;
    }

    public ValueTask SendMessageAsync(string sourceId, object message, string? targetId = null,
        CancellationToken cancellationToken = default)
    {
        if (!QueuedMessages.TryGetValue(sourceId, out var deliveryQueue)) QueuedMessages[sourceId] = deliveryQueue = [];

        deliveryQueue.Add(new MessageEnvelope(message, sourceId, targetId: targetId));
        return default;
    }

    public ValueTask ForwardWorkflowEventAsync(WorkflowEvent workflowEvent,
        CancellationToken cancellationToken = default)
    {
        return AddEventAsync(workflowEvent, cancellationToken);
    }

    ValueTask<StepContext> IRunnerContext.AdvanceAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    ValueTask<Executor> IRunnerContext.EnsureExecutorAsync(string executorId, IStepTracer? tracer,
        CancellationToken cancellationToken)
    {
        return new ValueTask<Executor>(Executors[executorId]);
    }

    ValueTask ISuperStepJoinContext.SendMessageAsync<TMessage>(string senderId, [DisallowNull] TMessage message,
        CancellationToken cancellationToken)
    {
        return SendMessageAsync(senderId, message, cancellationToken: cancellationToken);
    }

    ValueTask ISuperStepJoinContext.YieldOutputAsync<TOutput>(string senderId, [DisallowNull] TOutput output,
        CancellationToken cancellationToken)
    {
        return YieldOutputAsync(senderId, output, cancellationToken);
    }

    ValueTask<string> ISuperStepJoinContext.AttachSuperstepAsync(ISuperStepRunner superStepRunner,
        CancellationToken cancellationToken)
    {
        return new ValueTask<string>(string.Empty);
    }

    ValueTask<bool> ISuperStepJoinContext.DetachSuperstepAsync(string joinId)
    {
        return new ValueTask<bool>(false);
    }

    internal TestRunContext ConfigureExecutor(Executor executor)
    {
        _ = executor.DescribeProtocol();
        Executors.Add(executor.Id, executor);
        return this;
    }

    internal TestRunContext ConfigureExecutors(IEnumerable<Executor> executors)
    {
        foreach (var executor in executors) ConfigureExecutor(executor);

        return this;
    }

    public ValueTask YieldOutputAsync(string sourceId, object output, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (!QueuedOutputs.TryGetValue(sourceId, out var outputQueue)) QueuedOutputs[sourceId] = outputQueue = [];

        outputQueue.Add(output);
        return default;
    }

    public ValueTask<IEnumerable<Type>> GetStartingExecutorInputTypesAsync(
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (Executors.TryGetValue(StartingExecutorId, out var executor))
            return new ValueTask<IEnumerable<Type>>(executor.InputTypes);

        throw new InvalidOperationException(
            $"No executor with ID '{StartingExecutorId}' is registered in this context.");
    }

    private sealed class BoundContext(
        string executorId,
        TestRunContext runnerContext,
        IReadOnlyDictionary<string, string>? traceContext) : IWorkflowContext
    {
        public bool ConcurrentRunsEnabled => runnerContext.ConcurrentRunsEnabled;

        public IReadOnlyDictionary<string, string>? TraceContext => traceContext;

        public ValueTask AddEventAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default)
        {
            return runnerContext.AddEventAsync(workflowEvent, cancellationToken);
        }

        public ValueTask YieldOutputAsync(object output, CancellationToken cancellationToken = default)
        {
            // Preserves the event-type semantics used by InProcessRunnerContext so
            // assertions against AgentResponseUpdateEvent / AgentResponseEvent keep working.
            if (output is AgentResponseUpdate update)
                return AddEventAsync(new AgentResponseUpdateEvent(executorId, update), cancellationToken);

            if (output is AgentResponse response)
                return AddEventAsync(new AgentResponseEvent(executorId, response), cancellationToken);

            return AddEventAsync(new WorkflowOutputEvent(output, executorId), cancellationToken);
        }

        public ValueTask RequestHaltAsync()
        {
            return AddEventAsync(new RequestHaltEvent());
        }

        public ValueTask QueueClearScopeAsync(string? scopeName = null, CancellationToken cancellationToken = default)
        {
            return default;
        }

        public ValueTask QueueStateUpdateAsync<T>(string key, T? value, string? scopeName = null,
            CancellationToken cancellationToken = default)
        {
            return default;
        }

        public ValueTask<T?> ReadStateAsync<T>(string key, string? scopeName = null,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<T?>(default(T?));
        }

        public ValueTask<HashSet<string>> ReadStateKeysAsync(string? scopeName = null,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<HashSet<string>>([]);
        }

        public ValueTask SendMessageAsync(object message, string? targetId = null,
            CancellationToken cancellationToken = default)
        {
            return runnerContext.SendMessageAsync(executorId, message, targetId, cancellationToken);
        }

        public ValueTask<T> ReadOrInitStateAsync<T>(string key, Func<T> initialStateFactory, string? scopeName = null,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<T>(initialStateFactory());
        }
    }
}