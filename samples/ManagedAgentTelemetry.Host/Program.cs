using ANcpLua.Agents.Hosting.AGUI.Durable;
using Azure.AI.Projects;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Search.Documents;
using ManagedAgentTelemetry.Host.Integrations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Graph;
using Qyl.DurableAgents;
using Qyl.DurableAgents.Generated;

var foundryEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT")
                      ?? throw new InvalidOperationException("AZURE_AI_FOUNDRY_ENDPOINT is required.");

var deploymentName = Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_DEPLOYMENT")
                     ?? "gpt-4o-mini";

var searchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT")
                     ?? throw new InvalidOperationException("AZURE_SEARCH_ENDPOINT is required.");

var searchIndex = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX")
                  ?? "runbooks";

Azure.Identity.AzureCliCredential credential = new();

SearchClient searchClient = new(new Uri(searchEndpoint), searchIndex, credential);
RunbookSearch runbookSearch = new(searchClient);
var runbookTool = AIFunctionFactory.Create(runbookSearch.SearchAsync);

AIAgent telemetryAssistant = new AIProjectClient(new Uri(foundryEndpoint), credential)
    .AsAIAgent(
        model: deploymentName,
        instructions:
            "You are the telemetry assistant for the operations team. " +
            "Given a question about a recent run, consult the runbook search tool " +
            "if procedures or post-incident reports may be relevant, then return " +
            "a concise, structured TelemetryReport.",
        name: "TelemetryAssistant",
        tools: [runbookTool]);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(_ => new GraphServiceClient(
    credential,
    scopes: ["https://graph.microsoft.com/.default"]));
builder.Services.AddSingleton<TeamsNotifier>();

builder.Services
    .AddOpenTelemetry()
    .UseAzureMonitor()
    .WithTracing(t => t.AddSource("Microsoft.Agents.AI", "Microsoft.Agents.AI.DurableTask"));

builder.Services.AddQylDurableAgents(options => options.AddAIAgent(telemetryAssistant));
builder.Services.AddQylDurableAgentStreaming();

var app = builder.Build();

QylActivityServices.Provider = app.Services;

app.MapQylAgentEndpoints();
app.MapQylDurableAgentStream();
app.MapQylDurableAgentStreamGrpc();

await app.RunAsync().ConfigureAwait(false);
