// Showcase: MAF ChatClientAgent  x  ANcpLua.Agents.Hosting.ServiceDefaults
//           (AddQylAgentServiceDefaults + MapQylAgentEndpoints)  x  ANcpLua.Agents.Testing FakeChatClient (offline).
//
// An ASP.NET Core host that wires the Aspire-style agent service defaults: a single
// builder.AddQylAgentServiceDefaults() call registers MAF/ANcpLua telemetry plus health
// checks, app.MapQylAgentEndpoints() maps the standard /health and /alive probes, and a
// minimal /run endpoint drives a FakeChatClient-backed agent and returns its text — no
// API key, no network. Build-only sample (the server is never started here).

using ANcpLua.Agents.Hosting.ServiceDefaults;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Registers MAF + ANcpLua agent telemetry and default health checks in one call.
builder.AddQylAgentServiceDefaults();

var app = builder.Build();

// Offline, key-free chat backend seeded with a canned reply.
using var chatClient = new FakeChatClient();
chatClient.WithResponse("Service-defaults demo agent — no live model is wired.");

// Agent run telemetry resolves from DI, so build it from app.Services after Build().
var agent = new ChatClientAgent(
        chatClient,
        name: "service-defaults-agent",
        instructions: "You are a key-free demo agent.")
    .AsBuilder()
    .UseAgentRunTelemetry()
    .Build(app.Services);

// Minimal endpoint: run the agent and return its text.
app.MapGet("/run", async () => (await agent.RunAsync("hello")).Text);

// Standard liveness/readiness probes: /health and /alive.
app.MapQylAgentEndpoints();

app.Run();
