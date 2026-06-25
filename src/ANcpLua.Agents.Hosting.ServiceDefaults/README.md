# ANcpLua.Agents.Hosting.ServiceDefaults

Aspire-style service defaults for Microsoft Agent Framework services.

Compatible with: Microsoft.Agents.AI 1.11.x
Tested against: Microsoft.Agents.AI 1.11.0

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

`ANcpLua.Agents.Instrumentation` owns run/tool telemetry. ServiceDefaults wires its source and meter registration helpers while staying limited to health endpoints and OpenTelemetry setup glue.
