// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/MessageDelivery.cs

// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace ANcpLua.Agents.Testing.Workflows.Internals;

internal sealed class MessageDelivery
{
    [JsonConstructor]
    internal MessageDelivery(MessageEnvelope envelope, string targetId)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(targetId);
        Envelope = envelope;
        TargetId = targetId;
    }

    internal MessageDelivery(MessageEnvelope envelope, Executor target)
        : this(envelope, target.Id)
    {
        ArgumentNullException.ThrowIfNull(target);
        TargetCache = target;
    }

    public string TargetId { get; }
    public MessageEnvelope Envelope { get; }

    [JsonIgnore] internal Executor? TargetCache { get; set; }
}