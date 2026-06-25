using System.ComponentModel;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddAIAgent("ticket-agent", (serviceProvider, _) =>
{
    var lookupStatus = AIFunctionFactory.Create(
        LookupStatusAsync,
        name: "lookup_status",
        description: "Look up the status of a support ticket.");

    var chatClient = FakeChatClient
        .WithFactory(_ => [new TextContent("DevUI demo agent — no live model is wired. Ask about a ticket to see a tool call.")])
        .WithResponse(
            [new FunctionCallContent("call_1", lookupStatus.Name, new Dictionary<string, object?> { ["ticket"] = "demo-123" })],
            ChatFinishReason.ToolCalls)
        .WithResponse("Ticket demo-123 is currently open.");

    return new ChatClientAgent(
            chatClient,
            name: "ticket-agent",
            instructions: "You look up support ticket status.",
            tools: [lookupStatus])
        .AsBuilder()
        .UseAgentTelemetry()
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
