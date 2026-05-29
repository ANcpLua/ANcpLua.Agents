# ANcpLua.Agents.Mcp.Hosting

Consumer toolkit for Microsoft Agent Framework — Qyl-prefixed facades over
`ModelContextProtocol.AspNetCore`. Lets you expose your Qyl agents and tools AS
an MCP server over Streamable HTTP, so other MAF (or any MCP-aware) clients can
consume them.

Compatible with: Microsoft.Agents.AI 1.8.x
Tested against: Microsoft.Agents.AI 1.8.0
Capability tested against: ModelContextProtocol.AspNetCore 1.3.0

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).

## Quick start

```csharp
using ANcpLua.Agents.Mcp.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

WebApplication app = builder.Build();
app.MapQylMcp();
app.Run();

[McpServerToolType]
public class Tools
{
    [McpServerTool(Name = "echo", Title = "Echo", ReadOnly = true, Idempotent = true)]
    [Description("Echoes the input back.")]
    public string Echo(string input) => input;
}
```

## Rich tool annotations

The `[McpServerTool]` attribute carries a full set of behavioral hints that MCP
clients use for safety filtering, retry strategy, and human-in-the-loop policy.
Set them explicitly per tool — they flow through MAF and MCP-native consumers
identically:

```csharp
[McpServerToolType]
public class MotorTools
{
    [McpServerTool(
        Name = "turn_left",
        Title = "Turn Left",
        ReadOnly = false,        // mutates physical state
        Destructive = true,      // movement cannot be undone trivially
        Idempotent = false,      // each call accumulates rotation
        OpenWorld = false,       // interacts only with the local robot
        TaskSupport = ToolTaskSupport.Optional),
     Description("Basic command: Turns the robot car anticlockwise.")]
    public async Task<string> TurnLeftAsync(
        [Description("The angle (in ° / degrees) to turn anticlockwise.")] int angle)
    {
        await Task.Delay(100);
        return $"turned anticlockwise {angle}°.";
    }

    [McpServerTool(
        Name = "stop",
        Title = "Stop",
        ReadOnly = false,
        Destructive = false,     // safe to call repeatedly
        Idempotent = true,       // stop+stop = stop
        OpenWorld = false,
        TaskSupport = ToolTaskSupport.Optional),
     Description("Basic command: Stops the robot car.")]
    public Task<string> StopAsync() => Task.FromResult("stopped.");
}
```

Set `TaskSupport = ToolTaskSupport.Optional` to let clients choose between the
fire-and-forget background task path and the standard synchronous call path; set
it to `Required` when the tool must always run as a background task (see below).

## Long-running tools with progress notifications

When `TaskSupport = ToolTaskSupport.Required`, the SDK schedules the tool as a
background task and surfaces progress to clients via `IProgress<ProgressNotificationValue>`.
The SDK injects the `IProgress` instance at invocation time — you don't register
it anywhere; it's a regular parameter:

```csharp
using ModelContextProtocol;
using ModelContextProtocol.Server;

[McpServerTool(
    Name = "run_diagnostics_with_progress",
    Title = "Run Diagnostics with Progress",
    ReadOnly = true,
    Idempotent = true,
    TaskSupport = ToolTaskSupport.Required),
 Description("Runs a full diagnostics check on all motors with progress reporting.")]
public static async Task<string> RunDiagnosticsWithProgressAsync(
    IProgress<ProgressNotificationValue> progress)
{
    for (int i = 1; i <= 4; i++)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        progress.Report(new() { Progress = i, Total = 4 });
    }

    return "Diagnostics complete. All 4 motors passed.";
}
```

Clients that consume this tool via `RunQylToolAsTaskAsync` (in the
`ANcpLua.Agents.Mcp` client package) will see each `progress.Report(...)` call
as a mid-flight `mcp.task.progress` span event — so the observability timeline
reflects the four diagnostic stages instead of a single fat span at the end.
