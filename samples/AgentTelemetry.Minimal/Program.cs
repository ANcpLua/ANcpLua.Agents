// Showcase: the no-qyl floor for agent telemetry — MAF-native gen_ai spans, zero custom code.
//
// MAF 1.13 emits conformant OpenTelemetry GenAI spans natively; you do NOT hand-roll any
// run/tool decorator. One call, .UseOpenTelemetry(), wraps the agent in OpenTelemetryAgent,
// which emits 'invoke_agent' spans. Because the OTel layer sits BELOW the framework's
// FunctionInvokingChatClient, the same source also gets 'execute_tool' spans for every tool
// call. Sensitive data (raw prompts / arguments / results) is OFF by default and pinned off
// explicitly.
//
// This is the MINIMAL telemetry sample: in-box OpenTelemetry only, no qyl dependency. The
// sibling AgentTelemetry.SemConv sample layers a typed qyl gen_ai.evaluation.* enrichment span
// on top of exactly this baseline.
//
// Everything is OFFLINE: the agent runs over FakeChatClient (no API key, no network). Set
// OTEL_EXPORTER_OTLP_ENDPOINT to export; otherwise telemetry is produced in-process and is
// still visible to any ActivityListener.

using System.ComponentModel;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// Default source/meter name MAF's OpenTelemetryAgent writes to (OpenTelemetryConsts.DefaultSourceName).
const string AgentSource = "Experimental.Microsoft.Agents.AI";

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource(AgentSource);
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            tracing.AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(AgentSource);
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            metrics.AddOtlpExporter();
    });

using var host = builder.Build();

// Starting the host runs OpenTelemetry's hosted service, which resolves the TracerProvider /
// MeterProvider and registers their ActivityListeners. Without this, StartActivity returns null
// and every span is silently dropped.
await host.StartAsync();

using var chatClient = new FakeChatClient();
chatClient
    .WithResponse(
        [new FunctionCallContent("call_1", "lookup_status", new Dictionary<string, object?> { ["ticket"] = "demo-123" })],
        ChatFinishReason.ToolCalls)
    .WithResponse("done");

var agent = new ChatClientAgent(
        chatClient,
        name: "local-agent",
        tools: [AIFunctionFactory.Create(LookupStatusAsync)])
    .AsBuilder()
    // MAF-native telemetry: invoke_agent + execute_tool spans, sensitive data off.
    .UseOpenTelemetry(configure: a => a.EnableSensitiveData = false)
    .Build(host.Services);

await agent.RunAsync("check demo ticket");

await host.StopAsync();

static Task<string> LookupStatusAsync(
    [Description("Ticket id.")] string ticket,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"status:{ticket}:open");
}
