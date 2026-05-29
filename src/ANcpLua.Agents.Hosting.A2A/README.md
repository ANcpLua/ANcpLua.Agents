# ANcpLua.Agents.Hosting.A2A

Preview-channel Qyl-prefixed facades over Microsoft Agent Framework Agent2Agent (A2A) protocol hosting.

Compatible with: Microsoft.Agents.AI 1.8.x
Tested against: Microsoft.Agents.AI.A2A / Microsoft.Agents.AI.Hosting.A2A / Microsoft.Agents.AI.Hosting.A2A.AspNetCore 1.7.0-preview.260526.1
Tested against: A2A / A2A.AspNetCore 1.0.0-preview2

Channel: preview. Keep this package isolated from stable consumers — the underlying MAF A2A surface ships under `MEAI001` experimental diagnostics.

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).

## Server — expose an `AIAgent` over A2A

```csharp
using ANcpLua.Agents.Hosting.A2A;
using A2A;

var builder = WebApplication.CreateBuilder(args);
AIAgent agent = /* construct your agent */;

builder.AddQylA2AServer(agent);

var app = builder.Build();

AgentCard card = new()
{
    Name = "FilesAgent",
    Description = "Handles requests relating to files",
    Version = "1.0.0",
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
};

app.MapQylA2A(agent, card, path: "/");

await app.RunAsync();
```

`MapQylA2A` is a one-shot composite: it calls `MapA2AJsonRpc(agent, path)` and `MapWellKnownAgentCard(card)` against the configured `IEndpointRouteBuilder`. Use the granular extensions (`MapQylA2AJsonRpc`, `MapQylA2AHttpJson`, `MapQylWellKnownAgentCard`) when finer control is needed.

## Client — consume a remote A2A agent as an `AIAgent`

```csharp
using ANcpLua.Agents.Hosting.A2A;

AIAgent remoteAgent = await QylA2AClientExtensions.ConnectQylA2AAsync(new Uri("http://localhost:5000"));

// Use as a tool of another agent, or call directly:
var response = await remoteAgent.RunAsync("What files are in folder 'Demo1'?");
```

`ConnectQylA2AAsync` wraps `A2ACardResolver.GetAIAgentAsync()` from the `Microsoft.Agents.AI.A2A` bridge so the consumer only needs a single URL.

---
Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Compatible with: Microsoft.Agents.AI 1.8.x
Tested against: Microsoft.Agents.AI 1.8.0
