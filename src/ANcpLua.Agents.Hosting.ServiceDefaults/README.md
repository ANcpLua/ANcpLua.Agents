# ANcpLua.Agents.Hosting.ServiceDefaults

Aspire-style service defaults pre-tuned for the Microsoft Agent Framework ActivitySource family.

## Surface

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddQylAgentServiceDefaults();   // health checks + MAF source registration helpers

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddQylAgentSources());  // Microsoft.Agents.AI + Experimental.MEAI

var app = builder.Build();
app.MapQylAgentEndpoints();             // /health (liveness) + /alive (readiness=false)
app.Run();
```

## What it does NOT do

Deliberately does not configure OTLP exporters, resilience policies, or service discovery — those are
opinionated choices best left to the consumer's own ServiceDefaults extension on top of this one.

---
Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Compatible with: Microsoft.Agents.AI 1.6.x
Tested against: Microsoft.Agents.AI 1.6.1
