# ANcpLua.Agents.Instrumentation

MAF-native OpenTelemetry registration helpers for Microsoft Agent Framework agents.

Compatible with: Microsoft.Agents.AI 1.11.x
Tested against: Microsoft.Agents.AI 1.11.0

Channel: stable. This package must not reference Microsoft Agent Framework preview, RC, or alpha packages.

## Surface

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAgentFrameworkSources())
    .WithMetrics(metrics => metrics.AddAgentFrameworkMeters());

var agent = baseAgent.AsBuilder()
    .UseAgentTelemetry()
    .Build();
```

MAF 1.11 emits semantic-convention telemetry natively: `UseAgentTelemetry` wraps the agent in `OpenTelemetryAgent` (`invoke_agent` spans), and `FunctionInvokingChatClient` adds `execute_tool` spans on the same source (`Experimental.Microsoft.Agents.AI`). This package no longer ships hand-rolled run/tool decorators — it registers that source and meter and pins sensitive data (raw prompts, message content, tool arguments and results) off by default.
