using System.ComponentModel;
using System.Diagnostics;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Qyl.OpenTelemetry.SemanticConventions.Incubating.Attributes.GenAi;

// Showcase: ANcpLua.Agents.Instrumentation + qyl typed OpenTelemetry GenAI semantic conventions.
// The agent's own activity/meter sources flow through AddAgentFrameworkSources()/Meters(); a manual
// span is tagged with strongly-typed gen_ai.* keys from Qyl.OpenTelemetry.SemanticConventions.Incubating
// (GenAiAttributes) instead of stringly-typed attribute names.
//
// Combination: MAF agent x ANcpLua.Agents.Instrumentation x Qyl.OpenTelemetry.SemanticConventions(.Incubating) x OTel.
// Set OTEL_EXPORTER_OTLP_ENDPOINT to export; otherwise telemetry is produced in-process.

const string ShowcaseSource = "AgentTelemetry.SemConv.Showcase";

var builder = Host.CreateApplicationBuilder(args);
builder.AddAgentTelemetry();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAgentFrameworkSources();
        tracing.AddSource(ShowcaseSource);
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
using var activitySource = new ActivitySource(ShowcaseSource);

using var chatClient = new FakeChatClient();
chatClient
    .WithResponse(
        [new FunctionCallContent("call_1", "lookup_status", new Dictionary<string, object?> { ["ticket"] = "demo-123" })],
        ChatFinishReason.ToolCalls)
    .WithResponse("Ticket demo-123 is open.");

var agent = new ChatClientAgent(
        chatClient,
        name: "support-agent",
        tools: [AIFunctionFactory.Create(LookupStatusAsync)])
    .AsBuilder()
    .UseAgentRunTelemetry()
    .UseAgentToolTelemetry()
    .Build(host.Services);

using (var activity = activitySource.StartActivity("invoke_agent support-agent"))
{
    activity?.SetTag(GenAiAttributes.OperationName, "invoke_agent");
    // gen_ai.provider.name — the typed const surfaces the OTel semconv rename from the
    // deprecated gen_ai.system, so the showcase stays conformant by construction.
    activity?.SetTag(GenAiAttributes.ProviderName, "ancplua.agents");
    activity?.SetTag(GenAiAttributes.AgentName, "support-agent");
    activity?.SetTag(GenAiAttributes.ConversationId, Guid.NewGuid().ToString("n"));

    var response = await agent.RunAsync("check demo ticket", cancellationToken: CancellationToken.None);

    activity?.SetTag(GenAiAttributes.ToolName, "lookup_status");
    Console.WriteLine(response.Text);
}

static Task<string> LookupStatusAsync(
    [Description("Ticket id.")] string ticket,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"status:{ticket}:open");
}
