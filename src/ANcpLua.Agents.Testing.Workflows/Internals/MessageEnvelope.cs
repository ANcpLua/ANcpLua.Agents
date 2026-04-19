// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/MessageEnvelope.cs

// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace ANcpLua.Agents.Testing.Workflows.Internals;

internal sealed class MessageEnvelope(
    object message,
    ExecutorIdentity source,
    TypeId? declaredType = null,
    string? targetId = null,
    Dictionary<string, string>? traceContext = null)
{
    internal MessageEnvelope(
        object message,
        ExecutorIdentity source,
        Type declaredType,
        string? targetId = null,
        Dictionary<string, string>? traceContext = null) : this(message, source, new TypeId(declaredType), targetId,
        traceContext)
    {
        if (!declaredType.IsInstanceOfType(message))
            throw new ArgumentException(
                $"The declared type {declaredType} is not compatible with the message instance of type {message.GetType()}");
    }

    public TypeId MessageType => declaredType ?? new TypeId(message.GetType());
    public object Message => message;
    public ExecutorIdentity Source => source;
    public string? TargetId => targetId;

    public Dictionary<string, string>? TraceContext => traceContext;

    [MemberNotNullWhen(false, nameof(SourceId))]
    public bool IsExternal => Source == ExecutorIdentity.None;

    public string? SourceId => Source.Id;
}