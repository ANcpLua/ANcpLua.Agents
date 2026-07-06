# ANcpLua.Agents — Combination Showcase

Runnable samples demonstrating every meaningful combination of **Microsoft Agent Framework (MAF) 1.13.0**,
the **ANcpLua.Agents** helper layers, and **qyl** building blocks. Every sample is **offline** — it runs
over `ANcpLua.Agents.Testing.ChatClients.FakeChatClient`, needs no API keys, and is CI-safe. Four one-combinator combinations (chain, switch, conditional tools, structured-output enum round-trip) are asserted offline in `AgentTesting.Harness` rather than shipped as standalone apps.

```bash
dotnet build ANcpLua.Agents.slnx              # all samples
dotnet run  --project samples/AgentBuilder.Fluent
dotnet test samples/AgentTesting.Harness/AgentTesting.Harness.csproj
```

## Combination matrix

| Sample | MAF surface | ANcpLua.Agents | qyl (live) | Kind |
|---|---|---|---|---|
| **AgentBuilder.Fluent** | `ChatClientAgent`, `RunAsync` | `QylAgentOptionsBuilder` | — | exe |
| **AgentTools.Governed** | `AIFunctionFactory`, tool loop | `QylToolSet.From`, `UseQylGovernance` (budget · concurrency · capability) | — | exe |
| **AgentApproval.Gate** | `FunctionInvocation` | `QylApprovalGate` / `UseQylApproval`, `AgentApprovalDeniedException` | — | exe |
| **AgentGovernance.Lineage** | multi-agent run | `AgentCallLineage`, `AgentSpawnTracker`, `AgentCallGuard` | — | exe |
| **AgentTelemetry.Minimal** | agent spans/meters | MAF-native `.UseOpenTelemetry` (`invoke_agent`/`execute_tool`) | — | exe |
| **AgentTelemetry.SemConv** | agent spans | MAF-native `.UseOpenTelemetry` (`invoke_agent`/`execute_tool`) | **SemanticConventions(.Incubating)** `gen_ai.evaluation.*` enrichment span | exe |
| **AgentTelemetry.AutoInstrumented** | agent spans + hosting | `AddQylAgentServiceDefaults`, MAF-native `.UseOpenTelemetry` | **AutoInstrumentation.Hosting** zero-code source-interceptor telemetry | web |
| **AgentApiContracts** | structured output | `RunQylWithSchemaAsync<T>` | **Qyl.Api.Contracts** DTO | exe |
| **AgentServiceDefaults.Web** | MAF Hosting | `AddQylAgentServiceDefaults`, `MapQylAgentEndpoints` | — | web |
| **AgentDevUI** | DevUI + OpenAI endpoints | `.Instrumentation`, `.Testing` | — | web |
| **AgentDevUI.Governed** | DevUI + OpenAI endpoints | `UseQylGovernance` + MAF-native `.UseOpenTelemetry` (`invoke_agent`/`execute_tool`) | — | web |
| **AgentWorkflow.HumanInLoop** | `Workflows` checkpoint/resume | `AddQylHumanInTheLoop` | — | exe |
| **AgentWorkflow.TriageAutofix** | `Workflows` raw graph: conditioned `AddEdge<T>` edges · `IResettableExecutor` · structured `AIAgent.RunAsync<T>` · custom `WorkflowEvent` | `QylAgentOptionsBuilder` over `.Testing` `FakeChatClient` | — | exe |
| **AgentWorkflow.Declarative** | `Workflows.Declarative` (YAML) | `QylDeclarativeAgent.Build` | — | exe |
| **AgentTesting.Harness** | — | `.Testing` (`AgentRunHarness`, `FakeChatClient`, `ActivityCollector`/`ActivityAssert`) + `.Testing.Workflows` (`WorkflowFixture<T>`); also asserts the folded rows — chain ordering, switch routing, conditional-tool exposure, structured-output enum round-trip | — | test |
| **AgentWorkflow.Generators.Tested** | `ExecutorRouteGenerator` (MAF source generator) | `ANcpLua.Roslyn.Utilities.Testing` (`GeneratorResult`, `GeneratorCachingReport`, `Compile`) + `.Testing.Aot` (`AotRuntime`) | — | test |

## Notable findings surfaced by the showcase

- **`AgentWorkflow.Generators.Tested`** drives MAF's own `ExecutorRouteGenerator` through ANcpLua's generator
  test harness. The harness reports that MAF's generator **holds Roslyn symbols in its incremental pipeline
  state** (ANcpLua's forbidden-type analyzer flags them) — a real incrementality characteristic, surfaced
  rather than asserted away. The generated executor route is then compiled against real MAF types via
  `Compile`, and AOT posture is observed via `AotRuntime`.
- **`AgentTelemetry.SemConv`** gets its `invoke_agent` / `execute_tool` framework spans from MAF's native
  `.UseOpenTelemetry()` (no hand-rolled decorators); qyl's Incubating `GenAiAttributes` constants earn their
  place only on the one `gen_ai.evaluation.*` enrichment span MAF does not emit, surfacing the OTel semantic-
  convention rename `gen_ai.system` → `gen_ai.provider.name` at compile time — staying conformant by construction.
- **`AgentTools.Governed`** proves governance is real, not cosmetic: a capability-gated tool body runs **0**
  times when the capability is not granted, and a `MaxAttempts=1` budget trips `AgentBudgetExceededException`
  on the second call (body runs exactly once).

## Live combination: qyl AutoInstrumentation

The **Qyl.OpenTelemetry.AutoInstrumentation** suite (zero-code, AOT-native instrumentation for HttpClient,
EF Core, SqlClient, messaging, …) is published to nuget.org as of 4.0.x, so **`AgentTelemetry.AutoInstrumented`**
consumes it live — previously this combination could only be documented here as a pattern:

```csharp
builder.Services.AddQylAutoInstrumentation();   // hosting wiring; [ModuleInitializer] activates the interceptors
builder.AddQylAgentServiceDefaults();           // ANcpLua agent + MAF activity sources
builder.Services.AddOpenTelemetry().WithTracing(t => t
    .AddSource("Experimental.Microsoft.Agents.AI")   // MAF invoke_agent / execute_tool spans
    .AddSource(QylActivitySource.Name));             // qyl zero-code HTTP/DB/messaging spans
// Agent HTTP/DB calls are traced with zero per-call code, alongside the agent's gen_ai.* spans.
```

## Conventions

All samples: `Sdk="ANcpLua.NET.Sdk"` (or `.Test`), `IsPackable=false`, central package management, registered
in `ANcpLua.Agents.slnx`. See [`docs/showcase/DESIGN.md`](../docs/showcase/DESIGN.md) for the design spec and
[`docs/showcase/maf-nuget-channel-matrix.md`](../docs/showcase/maf-nuget-channel-matrix.md) for why DevUI/Hosting
are pinned to preview/alpha.
