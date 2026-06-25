using System.ComponentModel;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

builder.AddAgentTelemetry();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAgentFrameworkSources();

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            tracing.AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAgentFrameworkMeters();

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            metrics.AddOtlpExporter();
    });

using var host = builder.Build();

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
    .UseAgentRunTelemetry()
    .UseAgentToolTelemetry()
    .Build(host.Services);

await agent.RunAsync("check demo ticket", cancellationToken: CancellationToken.None);

static Task<string> LookupStatusAsync(
    [Description("Ticket id.")] string ticket,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"status:{ticket}:open");
}
