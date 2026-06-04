[![CI](https://github.com/ANcpLua/ANcpLua.Agents/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/ANcpLua/ANcpLua.Agents/actions/workflows/nuget-publish.yml)
[![NuGet ANcpLua.Agents](https://img.shields.io/nuget/v/ANcpLua.Agents?label=ANcpLua.Agents&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents/)
[![NuGet ANcpLua.Agents.Instrumentation](https://img.shields.io/nuget/v/ANcpLua.Agents.Instrumentation?label=.Instrumentation&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents.Instrumentation/)
[![NuGet ANcpLua.Agents.Workflows](https://img.shields.io/nuget/v/ANcpLua.Agents.Workflows?label=.Workflows&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents.Workflows/)
[![NuGet ANcpLua.Agents.Workflows.Declarative](https://img.shields.io/nuget/v/ANcpLua.Agents.Workflows.Declarative?label=.Workflows.Declarative&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents.Workflows.Declarative/)
[![NuGet ANcpLua.Agents.Testing](https://img.shields.io/nuget/v/ANcpLua.Agents.Testing?label=.Testing&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents.Testing/)
[![NuGet ANcpLua.Agents.Testing.Workflows](https://img.shields.io/nuget/v/ANcpLua.Agents.Testing.Workflows?label=.Testing.Workflows&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents.Testing.Workflows/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Agents

Lean toolkit for Microsoft Agent Framework 1.9.x.

The repo is intentionally small: runtime governance primitives, Agent Framework OpenTelemetry middleware, service defaults, workflow helpers, and test infrastructure. Provider-specific facades, MCP wrappers, Qyl Durable experiments, and demo product hosts were removed instead of kept alive as compatibility shims.

Compatible with: Microsoft.Agents.AI 1.9.x
Tested against: Microsoft.Agents.AI 1.9.0

## Packages

| Package | Contents |
|---|---|
| [`ANcpLua.Agents`](https://www.nuget.org/packages/ANcpLua.Agents/) | Core runtime helpers and governance primitives |
| [`ANcpLua.Agents.Instrumentation`](https://www.nuget.org/packages/ANcpLua.Agents.Instrumentation/) | MAF run/tool telemetry middleware and OpenTelemetry registration helpers |
| [`ANcpLua.Agents.Hosting.ServiceDefaults`](https://www.nuget.org/packages/ANcpLua.Agents.Hosting.ServiceDefaults/) | Health endpoints plus MAF ActivitySource registration helpers |
| [`ANcpLua.Agents.Workflows`](https://www.nuget.org/packages/ANcpLua.Agents.Workflows/) | Workflow facades and execution helpers |
| [`ANcpLua.Agents.Workflows.Declarative`](https://www.nuget.org/packages/ANcpLua.Agents.Workflows.Declarative/) | Stable declarative workflow file/string helpers |
| [`ANcpLua.Agents.Testing`](https://www.nuget.org/packages/ANcpLua.Agents.Testing/) | Fake agents, fake chat clients, diagnostics, conformance fixtures |
| [`ANcpLua.Agents.Testing.Workflows`](https://www.nuget.org/packages/ANcpLua.Agents.Testing.Workflows/) | Workflow fixtures and workflow harnesses |

## Instrumentation

```csharp
builder.AddAgentTelemetry();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAgentFrameworkSources())
    .WithMetrics(metrics => metrics.AddAgentFrameworkMeters());

var agent = baseAgent.AsBuilder()
    .UseAgentRunTelemetry()
    .UseAgentToolTelemetry()
    .Build();
```

The telemetry package instruments Agent Framework middleware. It emits bounded run/tool spans and bounded metrics. It does not emit raw prompts, message content, tool arguments, tool results, API keys, or exception messages.

Siblings: [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) · [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) · [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers)
