// Showcase: QylAgentFactory  x  ANcpLua.Agents.Hosting.ServiceDefaults
//           (AddQylAgentServiceDefaults + MapQylAgentEndpoints)  x  ANcpLua.Agents.Testing FakeChatClient (offline).
//
// An ASP.NET Core host that wires the Aspire-style agent service defaults: a single
// builder.AddQylAgentServiceDefaults() call registers default health checks, and
// app.MapQylAgentEndpoints() maps the standard /health and /alive probes. A minimal
// /run endpoint drives a FakeChatClient-backed agent and returns its text — no API key,
// no network. Build-only sample (the server is never started here).
//
// Telemetry: QylAgentFactory always adds MAF-native OpenTelemetry, which emits semconv
// 'invoke_agent' spans (and, because OTel sits below FunctionInvokingChatClient, 'execute_tool'
// spans) on the "Experimental.Microsoft.Agents.AI" source. The factory pins sensitive data off.
// Register that source on a TracerProvider to export; this build-only sample produces spans
// in-process.

using ANcpLua.Agents.Hosting.ServiceDefaults;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Registers default health checks for the standard /health and /alive probes.
builder.AddQylAgentServiceDefaults();

var app = builder.Build();

// Offline, key-free chat backend seeded with a canned reply.
using var chatClient = new FakeChatClient();
chatClient.WithResponse("Service-defaults demo agent — no live model is wired.");

// The Qyl-owned factory adds the bounded telemetry wrapper and resolves middleware dependencies
// from app.Services.
var agent = QylAgentFactory.Create(
    chatClient,
    static options => options
        .WithName("service-defaults-agent")
        .WithInstructions("You are a key-free demo agent."),
    services: app.Services);

// Minimal endpoint: run the agent and return its text.
app.MapGet("/run", async () => (await agent.RunAsync("hello")).Text);

// Standard liveness/readiness probes: /health and /alive.
app.MapQylAgentEndpoints();

app.Run();
