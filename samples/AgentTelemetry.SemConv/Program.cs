// Showcase: MAF-native OpenTelemetry x Qyl typed gen_ai.* semantic conventions (Incubating).
//
// THE flagship telemetry sample. Two layers, cleanly separated:
//
//   1. FRAMEWORK SPANS (MAF, not hand-rolled): .UseOpenTelemetry() wraps the agent in
//      OpenTelemetryAgent, which emits conformant gen_ai 'invoke_agent' spans. Because the
//      OTel layer sits BELOW the framework's FunctionInvokingChatClient, the same source also
//      gets 'execute_tool' spans for every tool call. Sensitive data (raw prompts / arguments /
//      results) is OFF by default and pinned off explicitly. We do NOT recreate these spans.
//
//   2. ONE ENRICHMENT SPAN (qyl earns its place): MAF has no concept of response evaluation, so
//      after RunAsync returns we open a single custom 'evaluate_response' span and tag it with
//      strongly-typed gen_ai.evaluation.* keys from Qyl.OpenTelemetry.SemanticConventions.Incubating
//      (GenAiAttributes). These keys do not duplicate anything MAF emits — that is the whole point
//      of taking the Incubating dependency: typed, conformant-by-construction attribute names for
//      the semantic-convention surface the framework leaves to the application.
//
// Everything is OFFLINE: the agent runs over FakeChatClient (no API key, no network).
// Set OTEL_EXPORTER_OTLP_ENDPOINT to export; otherwise telemetry is produced in-process and is
// still visible to any ActivityListener.

using System.ComponentModel;
using System.Diagnostics;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Qyl.OpenTelemetry.SemanticConventions.Incubating.Attributes.GenAi;

// Default source/meter name MAF's OpenTelemetryAgent writes to (OpenTelemetryConsts.DefaultSourceName).
const string AgentSource = "Experimental.Microsoft.Agents.AI";
// Our own source for the single evaluation span layered on top of the framework spans.
const string EvalSource = "AgentTelemetry.SemConv.Eval";

var builder = Host.CreateApplicationBuilder(args);

// Collect both the framework's native gen_ai spans/metrics and our evaluation span. Export to
// OTLP when an endpoint is configured; otherwise telemetry is produced in-process.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource(AgentSource);
        tracing.AddSource(EvalSource);
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

using var evalSource = new ActivitySource(EvalSource);

using var chatClient = new FakeChatClient();
chatClient
    .WithResponse(
        [new FunctionCallContent("call_1", "lookup_status", new Dictionary<string, object?> { ["ticket"] = "demo-123" })],
        ChatFinishReason.ToolCalls)
    .WithResponse("Ticket demo-123 is open.");

var agent = new ChatClientAgent(
        chatClient,
        name: "support-agent",
        instructions: "You look up support ticket status.",
        tools: [AIFunctionFactory.Create(LookupStatusAsync, name: "lookup_status")])
    .AsBuilder()
    // MAF-native telemetry: invoke_agent + execute_tool spans, sensitive data off.
    .UseOpenTelemetry(configure: a => a.EnableSensitiveData = false)
    .Build(host.Services);

// The framework owns the invoke_agent / execute_tool spans created inside RunAsync.
var response = await agent.RunAsync("check demo ticket");
Console.WriteLine(response.Text);

// ENRICHMENT: one evaluation span the framework does not emit. A trivial offline heuristic stands
// in for a real evaluator (LLM-as-judge, regex grader, etc.); the value of the dependency is the
// typed, conformant gen_ai.evaluation.* attribute names — not the scoring logic.
using (var activity = evalSource.StartActivity("evaluate_response support-agent"))
{
    var mentionsTicket = response.Text.Contains("demo-123", StringComparison.OrdinalIgnoreCase);

    activity?.SetTag(GenAiAttributes.AgentName, "support-agent");
    activity?.SetTag(GenAiAttributes.EvaluationName, "ticket_reference_grounding");
    activity?.SetTag(GenAiAttributes.EvaluationScoreValue, mentionsTicket ? 1.0 : 0.0);
    activity?.SetTag(GenAiAttributes.EvaluationScoreLabel, mentionsTicket ? "grounded" : "ungrounded");
    activity?.SetTag(
        GenAiAttributes.EvaluationExplanation,
        mentionsTicket
            ? "Response cites the requested ticket id."
            : "Response does not reference the requested ticket id.");

    Console.WriteLine($"evaluation: ticket_reference_grounding = {(mentionsTicket ? "grounded" : "ungrounded")}");
}

await host.StopAsync();

static Task<string> LookupStatusAsync(
    [Description("Ticket id.")] string ticket,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"status:{ticket}:open");
}
