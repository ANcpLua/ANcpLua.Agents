// Showcase: MAF ChatClientAgent  x  ANcpLua.Agents.Hosting.ServiceDefaults
//           (AddQylAgentServiceDefaults + MapQylAgentEndpoints)  x  ANcpLua.Agents.Testing FakeChatClient (offline).
//
// An ASP.NET Core host that wires the Aspire-style agent service defaults: a single
// builder.AddQylAgentServiceDefaults() call registers default health checks, and
// app.MapQylAgentEndpoints() maps the standard /health and /alive probes. A minimal
// /run endpoint drives a FakeChatClient-backed agent and returns its text — no API key,
// no network. Build-only sample (the server is never started here).
//
// Telemetry: the agent is wrapped with MAF-native UseOpenTelemetry(), which emits semconv
// 'invoke_agent' spans (and, because OTel sits below FunctionInvokingChatClient, 'execute_tool'
// spans) on the "Experimental.Microsoft.Agents.AI" source. EnableSensitiveData is left at its
// default (false), set explicitly here so the bound stays visible. Register that source on a
// TracerProvider to export; this build-only sample produces spans in-process.

using ANcpLua.Agents.Hosting.ServiceDefaults;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Registers default health checks for the standard /health and /alive probes.
builder.AddQylAgentServiceDefaults();

var app = builder.Build();

// Offline, key-free chat backend seeded with a canned reply.
using var chatClient = new FakeChatClient();
chatClient.WithResponse("Service-defaults demo agent — no live model is wired.");

// MAF-native OpenTelemetry: 'invoke_agent' spans on "Experimental.Microsoft.Agents.AI".
// EnableSensitiveData defaults false; pinned explicitly to keep the prompt/response bound visible.
// Built from app.Services after Build() so the OTel middleware can resolve DI services.
var agent = new ChatClientAgent(
        chatClient,
        name: "service-defaults-agent",
        instructions: "You are a key-free demo agent.")
    .AsBuilder()
    .UseOpenTelemetry(configure: otel => otel.EnableSensitiveData = false)
    .Build(app.Services);

// Minimal endpoint: run the agent and return its text.
app.MapGet("/run", async () => (await agent.RunAsync("hello")).Text);

// Standard liveness/readiness probes: /health and /alive.
app.MapQylAgentEndpoints();

app.Run();
