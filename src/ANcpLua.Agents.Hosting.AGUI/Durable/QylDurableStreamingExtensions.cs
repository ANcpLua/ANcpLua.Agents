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
    ///     <see cref="IAgentResponseHandler"/> implementation with default
    ///     <see cref="DurableAgentStreamingOptions"/> (capacity 100, FullMode=Wait, 20s
    ///     SSE heartbeat). Call alongside <c>AddQylDurableAgents</c>.
    /// </summary>
    /// <param name="services">Target DI container.</param>
    /// <remarks>
    ///     Adds Grpc.AspNetCore services as well so callers can later opt in to the
    ///     <see cref="MapQylDurableAgentStreamGrpc"/> server-streaming endpoint without a second
    ///     wiring step. The gRPC server-side glue has no runtime cost when no gRPC endpoint is mapped.
    /// </remarks>
    public static IServiceCollection AddQylDurableAgentStreaming(this IServiceCollection services)
        => AddQylDurableAgentStreaming(services, configure: null);

    /// <summary>
    ///     Registers the durable-streaming surface with caller-supplied option overrides.
    /// </summary>
    /// <param name="services">Target DI container.</param>
    /// <param name="configure">
    ///     Callback that mutates the default <see cref="DurableAgentStreamingOptions"/> before
    ///     the singleton is registered. Pass <see langword="null"/> for defaults.
    /// </param>
    public static IServiceCollection AddQylDurableAgentStreaming(
        this IServiceCollection services,
        Action<DurableAgentStreamingOptions>? configure)
    {
        Guard.NotNull(services);

        var options = new DurableAgentStreamingOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
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
    ///     The channel is removed from the registry once its reader is naturally completed
    ///     (handler's <c>TryComplete</c> closes the writer side) or when the request cancels.
    ///     Cancellation propagates as <see cref="OperationCanceledException"/> — wrap with your
    ///     own middleware if you need a specific status code for "client disconnected before
    ///     the first update."
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
            DurableAgentStreamingOptions options,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers["X-Accel-Buffering"] = "no";

            var channel = registry.GetOrCreate(sessionKey);

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

            var heartbeatInterval = options.SseHeartbeatInterval;
            var heartbeatsEnabled = heartbeatInterval > TimeSpan.Zero && heartbeatInterval != Timeout.InfiniteTimeSpan;

            long messageCount = 0;
            var heartbeatTimer = heartbeatsEnabled ? new PeriodicTimer(heartbeatInterval) : null;
            try
            {
                // Both awaitables persist across iterations so neither is started twice in parallel:
                //   - waitToRead: avoids registering a fresh channel waiter every heartbeat (codex 0s6DMQPN)
                //   - heartbeat:  PeriodicTimer.WaitForNextTickAsync forbids concurrent pending calls
                //                 and throws InvalidOperationException if called again while one is
                //                 still outstanding. Persist until the tick is observed.
                Task<bool>? waitToRead = null;
                Task<bool>? heartbeat = null;
                while (true)
                {
                    waitToRead ??= channel.Reader.WaitToReadAsync(cancellationToken).AsTask();

                    if (heartbeatTimer is not null)
                    {
                        heartbeat ??= heartbeatTimer.WaitForNextTickAsync(cancellationToken).AsTask();
                        var winner = await Task.WhenAny(waitToRead, heartbeat).ConfigureAwait(false);
                        if (winner == heartbeat)
                        {
                            // Observe the result before nulling out, then schedule the next tick.
                            _ = await heartbeat.ConfigureAwait(false);
                            heartbeat = null;
                            // SSE comment frame — clients ignore it, proxies see traffic.
                            // Leave waitToRead pending; next iteration re-uses the same task.
                            await context.Response.WriteAsync(": keepalive\n\n", cancellationToken).ConfigureAwait(false);
                            await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                        // waitToRead won — leave heartbeat pending so we don't start a second one.
                    }

                    if (!await waitToRead.ConfigureAwait(false))
                    {
                        break; // channel completed
                    }
                    waitToRead = null; // consumed — next iteration creates a fresh waiter after the drain

                    while (channel.Reader.TryRead(out var update))
                    {
                        var json = JsonSerializer.Serialize(update);
                        await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
                        await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                        messageCount++;
                        StreamingTelemetry.MessagesConsumed.Add(1, transportTag, sessionTag);
                    }
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
                heartbeatTimer?.Dispose();
                activity?.SetTag(StreamingTelemetry.Tags.MessageCount, messageCount);
                registry.TryRemove(sessionKey);
            }
        });
    }

    /// <summary>
    ///     Maps the gRPC server-streaming surface for durable-agent updates. Pairs with
    ///     <see cref="AddQylDurableAgentStreaming(IServiceCollection)"/>. Clients invoke
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
