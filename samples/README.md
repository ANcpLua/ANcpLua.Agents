# ANcpLua.Agents — Combination Showcase

Runnable samples demonstrating every meaningful combination of **Microsoft Agent Framework (MAF) 1.13.0**,
the **ANcpLua.Agents** helper layers, and **qyl** building blocks. Every sample **except `CoordinatorTeam.CostSplit`** is **offline** — it runs
over `ANcpLua.Agents.Testing.ChatClients.FakeChatClient`, needs no API keys, and is CI-safe. Four one-combinator combinations (chain, switch, conditional tools, structured-output enum round-trip) are asserted offline in `AgentTesting.Harness` rather than shipped as standalone apps. `CoordinatorTeam.CostSplit` is the one live-API sample — it calls the Anthropic Managed Agents API and needs `ANTHROPIC_API_KEY` (see its own README).

Every MAF chat-client agent in the retained samples is created by `QylAgentFactory`. The factory supplies DI, composes any governance/approval middleware, and always returns the mandatory MAF-native telemetry wrapper; samples never construct `ChatClientAgent` directly.

```bash
dotnet build ANcpLua.Agents.slnx              # all samples
dotnet run  --project samples/AgentBuilder.Fluent
dotnet test samples/AgentTesting.Harness/AgentTesting.Harness.csproj
```

## Combination matrix

| Sample | MAF surface | ANcpLua.Agents | qyl (live) | Kind |
|---|---|---|---|---|
| **AgentBuilder.Fluent** | `AIAgent`, `RunAsync` | `QylAgentFactory` + `QylAgentOptionsBuilder` configuration | — | exe |
| **AgentTools.Governed** | `AIFunctionFactory`, tool loop | `QylToolSet.From`, `UseQylGovernance` (budget · concurrency · capability) | — | exe |
| **AgentApproval.Gate** | `FunctionInvocation` | `QylApprovalGate` / `UseQylApproval`, `AgentApprovalDeniedException` | — | exe |
| **AgentGovernance.Lineage** | multi-agent run | `AgentCallLineage`, `AgentSpawnTracker`, `AgentCallGuard` | — | exe |
| **AgentTelemetry.SemConv** | agent spans/meters | `QylAgentFactory` mandatory telemetry (`invoke_agent`/`execute_tool`) | **SemanticConventions(.Incubating)** `gen_ai.evaluation.*` enrichment span | exe |
| **AgentTelemetry.AutoInstrumented** | agent spans + hosting | `QylAgentFactory`, `AddQylAgentServiceDefaults` | **AutoInstrumentation.Hosting** zero-code source-interceptor telemetry | web |
| **AgentApiContracts** | structured output | `RunQylWithSchemaAsync<T>` | **Qyl.Api.Contracts** DTO | exe |
| **AgentServiceDefaults.Web** | MAF Hosting | `AddQylAgentServiceDefaults`, `MapQylAgentEndpoints` | — | web |
| **AgentDevUI** | DevUI + OpenAI endpoints | `QylAgentFactory`, `.Testing` | — | web |
| **AgentDevUI.Governed** | DevUI + OpenAI endpoints | `QylAgentFactory` + `UseQylGovernance` | — | web |
| **AgentWorkflow.HumanInLoop** | `Workflows` checkpoint/resume | `AddQylHumanInTheLoop` | — | exe |
| **AgentWorkflow.TriageAutofix** | `Workflows` raw graph: conditioned `AddEdge<T>` edges · `IResettableExecutor` · structured `AIAgent.RunAsync<T>` · custom `WorkflowEvent` | `QylAgentFactory` over `.Testing` `FakeChatClient` | — | exe |
| **AgentWorkflow.Declarative** | `Workflows.Declarative` (YAML) | `QylDeclarativeAgent.Build` | — | exe |
| **AgentTesting.Harness** | — | `.Testing` (`AgentRunHarness`, `FakeChatClient`, `ActivityCollector`/`ActivityAssert`) + `.Testing.Workflows` (`WorkflowFixture<T>`); also asserts the folded rows — chain ordering, switch routing, conditional-tool exposure, structured-output enum round-trip | — | test |
| **AgentWorkflow.Generators.Tested** | `ExecutorRouteGenerator` (MAF source generator) | `ANcpLua.Roslyn.Utilities.Testing` (`GeneratorResult`, `GeneratorCachingReport`, `Compile`) + `.Testing.Aot` (`AotRuntime`) | — | test |
| **CoordinatorTeam.CostSplit** | Anthropic **Managed Agents** API — frontier coordinator + cheap workers (outside the MAF/ANcpLua/qyl triad) | — (standalone; official `Anthropic` SDK) | — | exe · **live API** |

## Notable findings surfaced by the showcase

- **`AgentWorkflow.Generators.Tested`** drives MAF's own `ExecutorRouteGenerator` through ANcpLua's generator
  test harness. The harness reports that MAF's generator **holds Roslyn symbols in its incremental pipeline
  state** (ANcpLua's forbidden-type analyzer flags them) — a real incrementality characteristic, surfaced
  rather than asserted away. The generated executor route is then compiled against real MAF types via
  `Compile`, and AOT posture is observed via `AotRuntime`.
- **`AgentTelemetry.SemConv`** gets its `invoke_agent` / `execute_tool` framework spans from the mandatory
  MAF-native wrapper installed by `QylAgentFactory` (no hand-rolled decorators); qyl's Incubating `GenAiAttributes` constants earn their
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
var app = builder.Build();
var agent = QylAgentFactory.Create(chatClient, options => options.WithName("agent"), services: app.Services);
// Agent HTTP/DB calls are traced with zero per-call code, alongside the agent's gen_ai.* spans.
```

## Conventions

All samples: `Sdk="ANcpLua.NET.Sdk"` (or `.Test`), `IsPackable=false`, central package management, registered
in `ANcpLua.Agents.slnx`. See [`docs/showcase/maf-nuget-channel-matrix.md`](../docs/showcase/maf-nuget-channel-matrix.md)
for why DevUI/Hosting are pinned to preview/alpha.
