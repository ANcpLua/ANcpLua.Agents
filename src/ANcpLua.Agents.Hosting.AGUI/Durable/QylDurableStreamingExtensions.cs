using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Channels;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DurableTask;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Grpc.AspNetCore.Server;

namespace ANcpLua.Agents.Hosting.AGUI.Durable;

/// <summary>
///     DI and endpoint wiring for the durable-agent side-channel streaming pattern. Combines a
///     per-session <see cref="DurableAgentStreamRegistry"/> with an <see cref="IAgentResponseHandler"/>
///     that publishes the durable orchestration's live update stream to that registry, and an
///     SSE endpoint that drains the channel back out to clients.
/// </summary>
/// <remarks>
///     <para>
///         The pattern works because MAF's <c>AgentEntity</c> resolves <see cref="IAgentResponseHandler"/>
///         from DI and, when present, invokes <see cref="IAgentResponseHandler.OnStreamingResponseUpdateAsync"/>
///         with the <em>live</em> inner-agent stream — not the synthesized post-aggregation stream
///         returned by <c>DurableAIAgent.RunCoreStreamingAsync</c>. The side-channel writes happen
///         inside the orchestration's AsyncLocal context, so at-least-once delivery on replay is
///         the cost of the pattern (see <see cref="DurableAgentStreamRegistry"/> docs).
///     </para>
/// </remarks>
public static class QylDurableStreamingExtensions
{
    /// <summary>
    ///     Registers the <see cref="DurableAgentStreamRegistry"/> and a side-channel
    ///     <see cref="IAgentResponseHandler"/> implementation. Call alongside <c>AddQylDurableAgents</c>.
    /// </summary>
    /// <remarks>
    ///     Adds Grpc.AspNetCore services as well so callers can later opt in to the
    ///     <see cref="MapQylDurableAgentStreamGrpc"/> server-streaming endpoint without a second
    ///     wiring step. The gRPC server-side glue has no runtime cost when no gRPC endpoint is mapped.
    /// </remarks>
    public static IServiceCollection AddQylDurableAgentStreaming(this IServiceCollection services)
    {
        Guard.NotNull(services);

        services.TryAddSingleton<DurableAgentStreamRegistry>();
        services.TryAddSingleton<IAgentResponseHandler, ChannelAgentResponseHandler>();
        services.AddGrpc();
        return services;
    }

    /// <summary>
    ///     Maps a Server-Sent-Events endpoint at <paramref name="pattern"/> that drains the
    ///     registry channel for <c>{sessionKey}</c>. The route MUST contain a
    ///     <c>{sessionKey}</c> segment. Emits one <c>data: &lt;json&gt;\n\n</c> frame per
    ///     <see cref="AgentResponseUpdate"/>.
    /// </summary>
    /// <remarks>
    ///     Returns 404 if the request cancels before the channel produces its first update; this
    ///     surfaces "no run was started for this session" without hanging the client. The channel
    ///     is removed from the registry once its reader is naturally completed.
    /// </remarks>
    public static IEndpointConventionBuilder MapQylDurableAgentStream(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern = "/agents/{sessionKey}/stream")
    {
        Guard.NotNull(endpoints);
        Guard.NotNullOrWhiteSpace(pattern);

        return endpoints.MapGet(pattern, async (
            string sessionKey,
            DurableAgentStreamRegistry registry,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers["X-Accel-Buffering"] = "no";

            Channel<AgentResponseUpdate> channel = registry.GetOrCreate(sessionKey);

            using var activity = StreamingTelemetry.ActivitySource.StartActivity(
                StreamingTelemetry.Spans.Subscribe,
                ActivityKind.Server);
            activity?.SetTag(StreamingTelemetry.Tags.SessionId, sessionKey);
            activity?.SetTag(StreamingTelemetry.Tags.Transport, StreamingTelemetry.Transports.Sse);

            var transportTag = new KeyValuePair<string, object?>(
                StreamingTelemetry.Tags.Transport,
                StreamingTelemetry.Transports.Sse);
            var sessionTag = new KeyValuePair<string, object?>(
                StreamingTelemetry.Tags.SessionId,
                sessionKey);

            long messageCount = 0;
            try
            {
                await foreach (var update in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    string json = JsonSerializer.Serialize(update);
                    await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
                    await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                    messageCount++;
                    StreamingTelemetry.MessagesConsumed.Add(1, transportTag, sessionTag);
                }
                activity?.SetTag(StreamingTelemetry.Tags.Outcome, StreamingTelemetry.Outcomes.Completed);
            }
            catch (OperationCanceledException)
            {
                activity?.SetTag(StreamingTelemetry.Tags.Outcome, StreamingTelemetry.Outcomes.Cancelled);
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
            catch (Exception ex)
            {
                activity?.SetTag(StreamingTelemetry.Tags.Outcome, StreamingTelemetry.Outcomes.Errored);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
            finally
            {
                activity?.SetTag(StreamingTelemetry.Tags.MessageCount, messageCount);
                registry.TryRemove(sessionKey);
            }
        });
    }

    /// <summary>
    ///     Maps the gRPC server-streaming surface for durable-agent updates. Pairs with
    ///     <see cref="AddQylDurableAgentStreaming"/>. Clients invoke
    ///     <c>AgentStream.Subscribe(session_key)</c> and receive a server-side stream of
    ///     <c>AgentUpdateMessage</c> until the orchestration completes (or the call is cancelled).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The gRPC service reads from the same <see cref="DurableAgentStreamRegistry"/> channel
    ///         that the SSE endpoint reads from, so both wire formats can coexist on the same host —
    ///         pick SSE for browsers, gRPC for service-to-service. Both terminate when the channel
    ///         writer side completes (handler's <c>finally</c>) or when the caller cancels.
    ///     </para>
    /// </remarks>
    public static GrpcServiceEndpointConventionBuilder MapQylDurableAgentStreamGrpc(this IEndpointRouteBuilder endpoints)
    {
        Guard.NotNull(endpoints);
        return endpoints.MapGrpcService<AgentStreamGrpcService>();
    }
}
