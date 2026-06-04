using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Instrumentation;

internal sealed class AgentRunTelemetryAgent : DelegatingAIAgent, IDisposable
{
    private readonly AgentTelemetryInstrumentation _telemetry;

    public AgentRunTelemetryAgent(AIAgent innerAgent, Action<AgentTelemetryOptions>? configure) : base(innerAgent)
    {
        _telemetry = AgentTelemetryInstrumentation.Create(configure);
    }

    public void Dispose()
    {
        _telemetry.Dispose();
    }

    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return _telemetry.TrackRunAsync(
            InnerAgent,
            () => InnerAgent.RunAsync(messages, session, options, cancellationToken));
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in _telemetry.TrackRunStreamingAsync(
                           InnerAgent,
                           () => InnerAgent.RunStreamingAsync(messages, session, options, cancellationToken),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }
}
