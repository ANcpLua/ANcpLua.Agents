// Showcase: QylAgentFactory  x  Qyl.OpenTelemetry.AutoInstrumentation.Hosting (LIVE nuget dependency)
//           x  ANcpLua.Agents.Hosting.ServiceDefaults  x  ANcpLua.Agents.Testing FakeChatClient (offline).
//
// Previously a documented-only combination: the Qyl AutoInstrumentation suite is published to
// nuget.org as of 4.0.x, so this sample now consumes it live. AddQylAutoInstrumentation()
// activates zero-code, AOT-native source-interceptor telemetry (HttpClient, ASP.NET Core,
// SqlClient, EF Core, ...) emitting on the "Qyl.OpenTelemetry.AutoInstrumentation"
// ActivitySource, while QylAgentFactory's mandatory MAF wrapper emits semconv 'invoke_agent' /
// 'execute_tool' spans on "Experimental.Microsoft.Agents.AI". One TracerProvider registers
// both sources, so agent spans and infrastructure spans export side by side with zero
// per-call instrumentation code. Build-only sample; the FakeChatClient keeps it key-free.

using ANcpLua.Agents.Hosting.ServiceDefaults;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using Qyl.OpenTelemetry.AutoInstrumentation;
using Qyl.OpenTelemetry.AutoInstrumentation.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Zero-code qyl auto-instrumentation — live nuget.org dependency (4.0.x). The
// [ModuleInitializer] bootstrap activates the source interceptors when the assembly loads;
// this call wires the hosting-level options and DI integration explicitly.
builder.Services.AddQylAutoInstrumentation();

// Aspire-style agent service defaults: default health checks for /health and /alive.
builder.AddQylAgentServiceDefaults();

// One TracerProvider carries both span families: MAF agent semconv spans and qyl's
// auto-instrumented infrastructure spans (HTTP/DB spans appear as soon as the app makes them).
builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing
    .AddSource("Experimental.Microsoft.Agents.AI")
    .AddSource(QylActivitySource.Name));

var app = builder.Build();

// Offline, key-free chat backend seeded with a canned reply.
using var chatClient = new FakeChatClient();
chatClient.WithResponse("Auto-instrumented demo agent — no live model is wired.");

// The Qyl-owned factory is the only construction boundary. It always adds MAF-native telemetry
// with sensitive data disabled and uses the host service provider for agent dependencies.
var agent = QylAgentFactory.Create(
    chatClient,
    static options => options
        .WithName("auto-instrumented-agent")
        .WithInstructions("You are a key-free demo agent."),
    services: app.Services);

// Minimal endpoint: run the agent and return its text.
app.MapGet("/run", async () => (await agent.RunAsync("hello")).Text);

// Standard liveness/readiness probes: /health and /alive.
app.MapQylAgentEndpoints();

app.Run();
