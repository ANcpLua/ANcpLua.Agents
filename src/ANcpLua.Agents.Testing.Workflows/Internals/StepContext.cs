// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/StepContext.cs
// TRIMMED: Removed ExportMessages/ImportMessages (depend on PortableMessageEnvelope, internal).
// Only QueuedMessages, HasMessages, and MessagesFor are needed by IRunnerContext.AdvanceAsync callers.

using System.Collections.Concurrent;

namespace ANcpLua.Agents.Testing.Workflows.Internals;

internal sealed class StepContext
{
    public ConcurrentDictionary<string, ConcurrentQueue<MessageEnvelope>> QueuedMessages { get; } = [];

    public bool HasMessages =>
        !QueuedMessages.IsEmpty && QueuedMessages.Values.Any(static messageQueue => !messageQueue.IsEmpty);

    public ConcurrentQueue<MessageEnvelope> MessagesFor(string target)
    {
        return QueuedMessages.GetOrAdd(target, static _ => new ConcurrentQueue<MessageEnvelope>());
    }
}