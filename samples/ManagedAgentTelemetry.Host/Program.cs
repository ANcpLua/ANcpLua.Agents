using Azure.AI.Projects;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Search.Documents;
using ManagedAgentTelemetry.Host.Integrations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Graph;
using Qyl.DurableAgents;
using Qyl.DurableAgents.Generated;

string foundryEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_FOUNDRY_ENDPOINT is required.");

string deploymentName = Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_DEPLOYMENT")
    ?? "gpt-4o-mini";

string searchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_SEARCH_ENDPOINT is required.");

string searchIndex = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX")
    ?? "runbooks";

// AzureCliCredential locally; switch to ManagedIdentityCredential in production.
Azure.Identity.AzureCliCredential credential = new();

// Tool: search internal runbooks via Azure AI Search.
SearchClient searchClient = new(new Uri(searchEndpoint), searchIndex, credential);
RunbookSearch runbookSearch = new(searchClient);
AIFunction runbookTool = AIFunctionFactory.Create(runbookSearch.SearchAsync);

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

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Microsoft Graph for Teams Adaptive Card posts (uses Microsoft 365 entitlement,
// which the team retains — this is the "free" Microsoft surface in our stack).
builder.Services.AddSingleton(_ => new GraphServiceClient(
    credential,
    scopes: ["https://graph.microsoft.com/.default"]));
builder.Services.AddSingleton<TeamsNotifier>();

builder.Services
    .AddOpenTelemetry()
    .UseAzureMonitor()
    .WithTracing(t => t.AddSource("Microsoft.Agents.AI", "Microsoft.Agents.AI.DurableTask"));

// Generated extension: configures the Durable Task worker + client (gRPC backend),
// registers every [QylOrchestrator] and [QylActivity] discovered in this assembly,
// and wires the AIAgent into the durable agent registry.
builder.Services.AddQylDurableAgents(options => options.AddAIAgent(telemetryAssistant));

WebApplication app = builder.Build();

// Hand the built service provider to the static accessor so [QylActivity] lambda
// bodies can resolve services (Durable Task's func-form activities have no
// IServiceProvider parameter on their delegate signature).
QylActivityServices.Provider = app.Services;

// Generated extension: maps every [QylAgentEndpoint] discovered in this assembly
// to a Minimal-API route that schedules the named orchestration.
app.MapQylAgentEndpoints();

await app.RunAsync().ConfigureAwait(false);
