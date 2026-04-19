// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/DeliveryMapping.cs

// Copyright (c) Microsoft. All rights reserved.

namespace ANcpLua.Agents.Testing.Workflows.Internals;

internal sealed class DeliveryMapping
{
    private readonly IEnumerable<MessageEnvelope> _envelopes;
    private readonly IEnumerable<Executor> _targets;

    public DeliveryMapping(IEnumerable<MessageEnvelope> envelopes, IEnumerable<Executor> targets)
    {
        ArgumentNullException.ThrowIfNull(envelopes);
        ArgumentNullException.ThrowIfNull(targets);
        _envelopes = envelopes;
        _targets = targets;
    }

    public DeliveryMapping(MessageEnvelope envelope, Executor target) : this([envelope], [target])
    {
    }

    public DeliveryMapping(MessageEnvelope envelope, IEnumerable<Executor> targets) : this([envelope], targets)
    {
    }

    public DeliveryMapping(IEnumerable<MessageEnvelope> envelopes, Executor target) : this(envelopes, [target])
    {
    }

    public IEnumerable<MessageDelivery> Deliveries => from target in _targets
        from envelope in _envelopes
        select new MessageDelivery(envelope, target);

    public void MapInto(StepContext nextStep)
    {
        foreach (var target in _targets)
        {
            var messageQueue = nextStep.MessagesFor(target.Id);
            foreach (var envelope in _envelopes) messageQueue.Enqueue(envelope);
        }
    }
}