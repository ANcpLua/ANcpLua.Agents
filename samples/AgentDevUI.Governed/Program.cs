// Showcase: MAF DevUI + OpenAI-compatible hosting
//   x ANcpLua.Agents.Governance (UseQylGovernance: capability + budget + concurrency enforcement)
//   x ANcpLua.Agents.Instrumentation (AddAgentTelemetry / UseAgentRunTelemetry / UseAgentToolTelemetry)
//   x qyl telemetry semantic conventions (wired through AddAgentTelemetry).
//
// Everything is OFFLINE: the agent is built over FakeChatClient (no API keys, no network).
// The governance middleware sits between the run/tool telemetry layers and the inner
// ChatClientAgent, so every tool invocation is checked against a per-tool AgentToolPolicy
// (required capabilities + attempt/tool-call budget + concurrency cap) before it executes.

using System.ComponentModel;
using ANcpLua.Agents.Governance;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// qyl-flavored Agent Framework telemetry (traces + metrics sources/meters).
builder.AddAgentTelemetry();

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

    return new ChatClientAgent(
            chatClient,
            name: "ticket-agent",
            instructions: "You look up support ticket status.",
            tools: [lookupStatus])
        .AsBuilder()
        .UseAgentRunTelemetry()
        .UseAgentToolTelemetry()
        // Governance wraps the inner agent's function invocation: capabilities are verified,
        // budget is reserved (rolled back on failure), and a concurrency slot is acquired
        // before each tool call runs.
        .UseQylGovernance(
            capabilities,
            budget,
            concurrency,
            policyResolver: name => name == lookupStatus.Name ? lookupPolicy : AgentToolPolicy.Permissive)
        .Build(serviceProvider);
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
