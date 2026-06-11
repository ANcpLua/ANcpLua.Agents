// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — WorkflowEvents.cs
//
// Modifications from upstream: keeps only the event projections that are public
// in the stable Microsoft.Agents.AI.Workflows 1.9.0 line and its stable
// declarative workflow companion.

namespace ANcpLua.Agents.Testing.Workflows.Framework;

/// <summary>
///     Strongly-typed projection of a flat <see cref="WorkflowEvent" /> list into the categories
///     that test assertions actually care about (executors, inputs, super-steps, errors).
///     Constructed once after a run completes — every property is a frozen <see cref="IReadOnlyList{T}" />.
/// </summary>
public sealed class WorkflowEvents
{
    public WorkflowEvents(IReadOnlyList<WorkflowEvent> workflowEvents)
    {
        Events = workflowEvents;
        EventCounts = workflowEvents.GroupBy(static e => e.GetType()).ToDictionary(static e => e.Key, static e => e.Count());
        ExecutorInvokeEvents = workflowEvents.OfType<ExecutorInvokedEvent>().ToList();
        ExecutorCompleteEvents = workflowEvents.OfType<ExecutorCompletedEvent>().ToList();
        InputEvents = workflowEvents.OfType<RequestInfoEvent>().ToList();
        SuperStepEvents = workflowEvents.OfType<SuperStepCompletedEvent>().ToList();
        ErrorEvents = workflowEvents.OfType<WorkflowErrorEvent>().ToList();
        OutputEvents = workflowEvents.OfType<WorkflowOutputEvent>().ToList();
        AgentResponseEvents = workflowEvents.OfType<AgentResponseEvent>().ToList();
        DeclarativeActionInvokeEvents = workflowEvents.OfType<DeclarativeActionInvokedEvent>().ToList();
        DeclarativeActionCompleteEvents = workflowEvents.OfType<DeclarativeActionCompletedEvent>().ToList();
        ConversationUpdateEvents = workflowEvents.OfType<ConversationUpdateEvent>().ToList();
    }

    public IReadOnlyList<WorkflowEvent> Events { get; }
    public IReadOnlyDictionary<Type, int> EventCounts { get; }
    public IReadOnlyList<ExecutorInvokedEvent> ExecutorInvokeEvents { get; }
    public IReadOnlyList<ExecutorCompletedEvent> ExecutorCompleteEvents { get; }
    public IReadOnlyList<RequestInfoEvent> InputEvents { get; }
    public IReadOnlyList<SuperStepCompletedEvent> SuperStepEvents { get; }
    public IReadOnlyList<WorkflowErrorEvent> ErrorEvents { get; }
    public IReadOnlyList<WorkflowOutputEvent> OutputEvents { get; }
    public IReadOnlyList<AgentResponseEvent> AgentResponseEvents { get; }
    public IReadOnlyList<DeclarativeActionInvokedEvent> DeclarativeActionInvokeEvents { get; }
    public IReadOnlyList<DeclarativeActionCompletedEvent> DeclarativeActionCompleteEvents { get; }
    public IReadOnlyList<ConversationUpdateEvent> ConversationUpdateEvents { get; }
}
