# ANcpLua.Agents.Instrumentation

Mandatory wrapped construction plus MAF-native OpenTelemetry registration helpers for Microsoft Agent Framework agents.

Compatible with: Microsoft.Agents.AI 1.13.x
Tested against: Microsoft.Agents.AI 1.13.0

Channel: stable. This package must not reference Microsoft Agent Framework preview, RC, or alpha packages.

## Surface

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAgentFrameworkSources())
    .WithMetrics(metrics => metrics.AddAgentFrameworkMeters());

var agent = QylAgentFactory.Create(
    chatClient,
    options => options.WithName("support-agent"),
    services: serviceProvider);
```

`QylAgentFactory` is the only supported construction path for Qyl chat-client agents. It creates the inner `ChatClientAgent` with the supplied DI provider, composes optional middleware, and returns `OpenTelemetryAgent` as the outermost wrapper. MAF 1.13 emits semantic-convention telemetry natively: the wrapper emits `invoke_agent` spans, and `FunctionInvokingChatClient` adds `execute_tool` spans on the same source (`Experimental.Microsoft.Agents.AI`). Sensitive data (raw prompts, message content, tool arguments and results) is pinned off. `UseAgentTelemetry` remains the lower-level path for wrapping an existing non-chat-client `AIAgent`.
