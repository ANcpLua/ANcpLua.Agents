# ANcpLua.Agents.Mcp.Hosting

Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Qyl-prefixed facades over `ModelContextProtocol.AspNetCore`. Lets you expose your
Qyl agents and tools AS an MCP server over Streamable HTTP, so other MAF (or any
MCP-aware) clients can consume them.

Compatible with: Microsoft.Agents.AI 1.6.x
Tested against: Microsoft.Agents.AI 1.6.1
Capability tested against: ModelContextProtocol.AspNetCore 1.3.0

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).

## Quick start

```csharp
using ANcpLua.Agents.Mcp.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddQylMcpServer();

WebApplication app = builder.Build();
app.MapQylMcp();
app.Run();

[McpServerToolType]
public class Tools
{
    [McpServerTool(Name = "echo", ReadOnly = true)]
    [Description("Echoes the input back.")]
    public string Echo(string input) => input;
}
```
