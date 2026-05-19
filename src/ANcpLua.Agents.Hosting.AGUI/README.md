# ANcpLua.Agents.Hosting.AGUI

Preview-channel Qyl-prefixed facades over Microsoft Agent Framework AG-UI (CopilotKit) streaming protocol support — server hosting plus client adapter in a single package.

Tested against `Microsoft.Agents.AI.AGUI` and `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` at `1.6.1-preview.260514.1`.

Channel: preview. Keep this package isolated from stable consumers.

## Server

```csharp
using ANcpLua.Agents.Hosting.AGUI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddQylAGUI();

WebApplication app = builder.Build();
app.MapQylAGUI("/", agent);

await app.RunAsync();
```

Multiple routes are supported — call `MapQylAGUI` once per agent:

```csharp
app.MapQylAGUI("/weather", weatherAgent);
app.MapQylAGUI("/movies", movieAgent);
```

## Client

```csharp
using ANcpLua.Agents.Hosting.AGUI;

HttpClient http = new();
AIAgent agent = http.AsQylAGUIAgent(
    new Uri("http://localhost:5000"),
    tools: [AIFunctionFactory.Create(ChangeColor, name: "change_color")]);

await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(messages))
{
    // ...
}
```

If you need the raw `IChatClient`:

```csharp
IChatClient client = http.AsQylAGUIChatClient(new Uri("http://localhost:5000"));
```

> AG-UI does not preserve agent `Instructions` or `Sessions` server-side — initialize and maintain conversation state on the client (see MAF samples).

---
Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Compatible with: Microsoft.Agents.AI 1.6.x
Tested against: Microsoft.Agents.AI 1.6.1
