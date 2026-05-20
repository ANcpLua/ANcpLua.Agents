# ANcpLua.Agents.Mcp

Consumer toolkit for Microsoft Agent Framework — Qyl-prefixed facades over the
official ModelContextProtocol .NET client. Lets a MAF agent consume tools,
resources, and long-running tasks exposed by any MCP server (GitHub Copilot MCP,
a sibling service hosting `ANcpLua.Agents.Mcp.Hosting`, a local stdio server,
etc.).

Compatible with: Microsoft.Agents.AI 1.6.x
Tested against: Microsoft.Agents.AI 1.6.1
Capability tested against: ModelContextProtocol 1.3.0

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).

## HTTP transport (remote MCP server)

```csharp
using ANcpLua.Agents.Mcp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

await using QylHttpMcpClient mcp = await QylMcpClientExtensions.CreateQylHttpMcpClientAsync(
    new Uri("https://api.githubcopilot.com/mcp/"));

IList<AITool> tools = await mcp.AsAIToolsAsync();

ChatClientAgent agent = chatClient.AsAIAgent(
    instructions: "You are a GitHub Expert",
    tools: tools);
```

The HTTP bundle owns both the `McpClient` and the underlying `HttpClientTransport`;
disposing the bundle disposes both in the correct order.

## Stdio transport (child-process MCP server)

```csharp
using ANcpLua.Agents.Mcp;

await using QylStdioMcpClient mcp = await QylMcpClientExtensions.CreateQylStdioMcpClientAsync(
    command: "dotnet",
    arguments: ["run", "--project", "../../MCPServerWithStdio"],
    name: "robot-car");

IList<AITool> tools = await mcp.AsAIToolsAsync();
```

Stdio is the one MCP transport where the server is a child process of the
consumer. Disposing `QylStdioMcpClient` terminates that child process — which
is what makes process-boundary metrics (RSS / CPU / fd-count) observable per
tool call. The MCP SDK's `McpClient` takes ownership of the transport, so this
bundle does not need a separate transport-disposal step.

## Resources and resource templates

Both bundles expose the raw `McpClient` via the `Client` property, so the full
SDK resource surface is available:

```csharp
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

// List the server's resource templates
IList<McpClientResourceTemplate> templates = await mcp.Client.ListResourceTemplatesAsync();
foreach (var template in templates)
    Console.WriteLine($"  {template.Name} {JsonSerializer.Serialize(template.ProtocolResourceTemplate)}");

// Fetch a static resource
var staticResource = await mcp.Client.ReadResourceAsync("resource://mcp/bio");
var staticText = (staticResource.Contents.FirstOrDefault() as TextResourceContents)?.Text;

// Fetch a parametrized template resource
var greetResource = await mcp.Client.ReadResourceAsync("resource://mcp/greet/Robby");
var greetText = (greetResource.Contents.FirstOrDefault() as TextResourceContents)?.Text;
```

`ListPromptsAsync`, `GetPromptAsync`, and `ListResourcesAsync` follow the same
pattern — they live on the SDK's `McpClient` directly.

## Long-running tools (OTel-instrumented lifecycle)

For tools declared with `TaskSupport.Required` (background execution + progress
notifications), `RunQylToolAsTaskAsync` wraps the full three-step SDK lifecycle
(`CallToolAsTaskAsync` → `PollTaskUntilCompleteAsync` → `GetTaskResultAsync`)
under one `ActivitySource` span:

```csharp
using System.Diagnostics;
using ANcpLua.Agents.Mcp;
using ModelContextProtocol;

ActivitySource source = new("MyApp");

JsonElement result = await mcp.Client.RunQylToolAsTaskAsync(
    toolName: "run_diagnostics_with_progress",
    arguments: new Dictionary<string, object?> { { "detailed", true } },
    source: source,
    observer: new Progress<ProgressNotificationValue>(v =>
        Console.WriteLine($"  PROGRESS: {v.Progress}/{v.Total}")));
```

Each progress notification adds an `mcp.task.progress` event with `progress`,
`total`, and `message` tags to the span, so the observability timeline reflects
server-reported state rather than a single fat span at the end. The
`observer` callback receives the same values verbatim — independent of OTel
emission — for any non-OTel UI or logging the caller needs.

> The MCP task API is marked experimental in SDK 1.3.0 (`MCPEXP001`).
> `QylMcpTaskExtensions` carries `[Experimental("MCPEXP001")]` so the marker
> propagates to callers at the call site — consumers acknowledge they're
> using an experimental SDK boundary and choose whether to suppress at their
> own callsite. The attribute will be removed when the SDK promotes the task
> API to stable.
