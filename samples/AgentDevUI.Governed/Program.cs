// Showcase: MAF DevUI + OpenAI-compatible hosting
//   x ANcpLua.Agents.Governance (UseQylGovernance: capability + budget + concurrency enforcement)
//   x QylAgentFactory's mandatory MAF-native OpenTelemetry wrapper.
//
// Everything is OFFLINE: the agent is built over FakeChatClient (no API keys, no network).
// Governance is the differentiator here: it sits between the MAF OpenTelemetry layer and the
// inner chat-client agent, so every tool invocation is checked against a per-tool AgentToolPolicy
// (required capabilities + attempt/tool-call budget + concurrency cap) before it executes.
//
// Telemetry is emitted natively by MAF: QylAgentFactory wraps the agent in OpenTelemetryAgent,
// producing conformant gen_ai 'invoke_agent' spans, and because OTel sits below the framework's
// FunctionInvokingChatClient it also gets 'execute_tool' spans on the same source. Sensitive data
// (raw prompts/arguments/results) is pinned off by the factory.

using System.ComponentModel;
using ANcpLua.Agents.Governance;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// Default source/meter name MAF's OpenTelemetryAgent writes to (OpenTelemetryConsts.DefaultSourceName).
const string AgentSource = AgentTelemetryExtensions.AgentFrameworkSourceName;

var builder = WebApplication.CreateBuilder(args);

// Collect the framework's native gen_ai spans/metrics. Export to OTLP when an endpoint is
// configured; otherwise telemetry is produced in-process (still visible to ActivityListeners).
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

builder.AddAIAgent("ticket-agent", (serviceProvider, _) =>
{
    var lookupStatus = AIFunctionFactory.Create(
        LookupStatusAsync,
        name: "lookup_status",
        description: "Look up the status of a support ticket.");

    var chatClient = FakeChatClient
        .WithFactory(_ => [new TextContent("Governed DevUI demo agent — no live model is wired. Ask about a ticket to see a governed tool call.")])
        .WithResponse(
            [new FunctionCallContent("call_1", lookupStatus.Name, new Dictionary<string, object?> { ["ticket"] = "demo-123" })],
            ChatFinishReason.ToolCalls)
        .WithResponse("Ticket demo-123 is currently open.");

    // Governance state. In a real app the capability context is registered scoped (per request);
    // here the offline demo grants the single capability the lookup tool requires.
    var capabilities = new AgentCapabilityContext(["tickets:read"]);
    var budget = new AgentBudgetEnforcer();
    var concurrency = new AgentConcurrencyLimiter(defaultLimit: 2);

    // Per-tool policy: lookup_status may be attempted up to 3x, 1 concurrent call,
    // and requires the "tickets:read" capability. Unknown tools fall back to permissive.
    var lookupPolicy = new AgentToolPolicy(
        MaxAttempts: 3,
        MaxToolCalls: 1,
        RequiredCapabilities: ["tickets:read"]);

    return QylAgentFactory.Create(
        chatClient,
        options => options
            .WithName("ticket-agent")
            .WithInstructions("You look up support ticket status.")
            .WithTools([lookupStatus]),
        pipeline => pipeline.UseQylGovernance(
            capabilities,
            budget,
            concurrency,
            policyResolver: name => name == lookupStatus.Name ? lookupPolicy : AgentToolPolicy.Permissive),
        services: serviceProvider);
});

builder.AddOpenAIResponses();
builder.AddOpenAIConversations();

if (builder.Environment.IsDevelopment())
    builder.AddDevUI();

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

if (app.Environment.IsDevelopment())
    app.MapDevUI(); // serves the playground at /devui

app.Run();

static Task<string> LookupStatusAsync(
    [Description("Ticket id.")] string ticket,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"status:{ticket}:open");
}
