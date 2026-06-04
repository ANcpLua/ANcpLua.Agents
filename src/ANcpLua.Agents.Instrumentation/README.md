# ANcpLua.Agents.Instrumentation

OpenTelemetry middleware for Microsoft Agent Framework agents.

Compatible with: Microsoft.Agents.AI 1.8.x
Tested against: Microsoft.Agents.AI 1.8.0

Channel: stable. This package must not reference Microsoft Agent Framework preview, RC, or alpha packages.

## Surface

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddAgentTelemetry();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAgentFrameworkSources())
    .WithMetrics(metrics => metrics.AddAgentFrameworkMeters());

var agent = baseAgent.AsBuilder()
    .UseAgentRunTelemetry()
    .UseAgentToolTelemetry()
    .Build();
```

The middleware emits bounded run and tool spans plus bounded counters and duration histograms. It never emits raw prompts, message content, tool arguments, tool results, API keys, or exception messages.
