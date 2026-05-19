# ANcpLua.Agents.Mcp

Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Qyl-prefixed facades over the official ModelContextProtocol .NET client. Lets a
MAF agent consume tools exposed by any MCP server (GitHub Copilot MCP, a sibling
service hosting `ANcpLua.Agents.Mcp.Hosting`, a local stdio server, etc.).

Compatible with: Microsoft.Agents.AI 1.6.x
Tested against: Microsoft.Agents.AI 1.6.1
Capability tested against: ModelContextProtocol 1.3.0

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).

## Quick start

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

The HTTP overload returns a `QylHttpMcpClient` bundle that owns both the `McpClient`
and the underlying `HttpClientTransport`; disposing the bundle disposes both in the
correct order. For non-HTTP transports (stdio, SSE, custom), construct the transport
yourself and pass it to `CreateQylMcpClientAsync(IClientTransport, ...)`.
