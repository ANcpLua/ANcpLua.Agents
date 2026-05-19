using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace ANcpLua.Agents.Hosting.AGUI.Durable;

/// <summary>
///     Centralises the <see cref="ActivitySource"/> and <see cref="Meter"/> the durable-streaming
///     surface emits to. Consumers enable spans by calling
///     <c>tracerBuilder.AddSource(<see cref="SourceName"/>)</c> and enable metrics by calling
///     <c>meterBuilder.AddMeter(<see cref="SourceName"/>)</c> on their OTel pipeline.
/// </summary>
/// <remarks>
///     <para>
///         Naming follows OTel's "library author" convention: the source name matches the
///         <c>ANcpLua.Agents.Hosting.AGUI.Durable</c> namespace one-for-one. The version tag is
///         populated from the assembly's <see cref="AssemblyInformationalVersionAttribute"/> so
///         consumers can correlate spans/metrics with a specific package version when
///         debugging a rollout.
///     </para>
///     <para>
///         Static state is intentional. Per OTel guidance for library instrumentation, the
///         <see cref="ActivitySource"/> / <see cref="Meter"/> live for the process and are
///         "on" only when a consumer attaches a listener — there is no runtime cost when no
///         consumer has subscribed.
///     </para>
/// </remarks>
internal static class StreamingTelemetry
{
    /// <summary>
    ///     ActivitySource / Meter name. Consumers add this to their OTel pipeline.
    /// </summary>
    public const string SourceName = "ANcpLua.Agents.Hosting.AGUI.Durable";

    private static string? AssemblyVersion() =>
        typeof(StreamingTelemetry).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    public static readonly ActivitySource ActivitySource = new(SourceName, AssemblyVersion());

    public static readonly Meter Meter = new(SourceName, AssemblyVersion());

    /// <summary>
    ///     Number of active per-session channels currently in the registry. Incremented when a
    ///     new session entry is created, decremented when <see cref="DurableAgentStreamRegistry.TryRemove"/>
    ///     succeeds. Steady-state value reflects the number of in-flight durable-agent streams.
    /// </summary>
    public static readonly UpDownCounter<long> ActiveSessions = Meter.CreateUpDownCounter<long>(
        "ancplua.agents.durable.active_sessions",
        unit: "{session}",
        description: "Number of active per-session channels in the durable-agent stream registry.");

    /// <summary>
    ///     Number of <see cref="Microsoft.Agents.AI.AgentResponseUpdate"/> messages written to a
    ///     session channel by the producer (the orchestration's response handler).
    /// </summary>
    public static readonly Counter<long> MessagesProduced = Meter.CreateCounter<long>(
        "ancplua.agents.durable.messages_produced",
        unit: "{message}",
        description: "Number of update messages written to a session channel by the producer.");

    /// <summary>
    ///     Number of <see cref="Microsoft.Agents.AI.AgentResponseUpdate"/> messages drained from a
    ///     session channel by a consumer. Tagged with <c>transport=sse|grpc</c> so dashboards can
    ///     break out by wire format.
    /// </summary>
    public static readonly Counter<long> MessagesConsumed = Meter.CreateCounter<long>(
        "ancplua.agents.durable.messages_consumed",
        unit: "{message}",
        description: "Number of update messages drained from a session channel by a consumer (tagged by transport).");

    /// <summary>
    ///     Standard tag names emitted on spans and counter measurements. Defined once so spelling
    ///     drift across producer/consumer code paths can't happen.
    /// </summary>
    public static class Tags
    {
        public const string SessionId = "ancplua.agents.session.id";
        public const string Transport = "ancplua.agents.transport";
        public const string MessageCount = "ancplua.agents.message.count";
        public const string Outcome = "ancplua.agents.outcome";
    }

    /// <summary>
    ///     Standard span operation names. Consumers can filter / route by these.
    /// </summary>
    public static class Spans
    {
        public const string Produce = "ANcpLua.Agents.Durable.Produce";
        public const string Subscribe = "ANcpLua.Agents.Durable.Subscribe";
    }

    /// <summary>
    ///     Transport tag values for <see cref="MessagesConsumed"/> and the <c>Subscribe</c> span.
    /// </summary>
    public static class Transports
    {
        public const string Sse = "sse";
        public const string Grpc = "grpc";
    }

    /// <summary>
    ///     Outcome tag values applied to spans on completion.
    /// </summary>
    public static class Outcomes
    {
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";
        public const string ConsumerDisconnect = "consumer_disconnect";
        public const string Errored = "errored";
    }
}
