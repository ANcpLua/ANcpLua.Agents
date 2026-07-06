# ANcpLua.Agents.Hosting.ServiceDefaults

Aspire-style service defaults for Microsoft Agent Framework services.

Compatible with: Microsoft.Agents.AI 1.13.x
Tested against: Microsoft.Agents.AI 1.13.0

Channel: stable. This package must not reference Microsoft Agent Framework preview, RC, or alpha packages.

## Surface

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddQylAgentServiceDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddQylAgentSources())
    .WithMetrics(metrics => metrics.AddQylAgentMeters());

var app = builder.Build();
app.MapQylAgentEndpoints();
app.Run();
```

Agent telemetry is MAF-native (`UseOpenTelemetry`). `AddQylAgentServiceDefaults` registers health checks only; the agent telemetry source and meter come from `AddQylAgentSources` / `AddQylAgentMeters`, which register the MAF `Experimental.Microsoft.Agents.AI` source via the `ANcpLua.Agents.Instrumentation` helpers. ServiceDefaults stays limited to health endpoints and OpenTelemetry setup glue.
