# ANcpLua.Agents — Combination Showcase (design spec)

Status: approved 2026-06-25. Base: MAF **1.11.0** bump (`deps:` commit). Branch: `feat/combination-showcase`.

## Goal

Demonstrate, with runnable projects, every meaningful combination of:

- **Microsoft Agent Framework 1.11.0** (`Microsoft.Agents.AI[.Abstractions/.Workflows/.Workflows.Declarative]`, DevUI/Hosting preview, `Workflows.Generators`)
- **ANcpLua.Agents** own layers (`.Instrumentation`, `.Workflows[.Declarative]`, `.Hosting.DevUI`, `.Hosting.ServiceDefaults`, `.Testing[.Workflows]`, and the core `ANcpLua.Agents` governance/builder/schema surface)
- **qyl** building blocks that restore from nuget.org: `Qyl.OpenTelemetry.SemanticConventions` + `.Incubating` 3.0.2, `Qyl.Api.Contracts` 0.2.0
- **ANcpLua.Roslyn.Utilities.Testing** (generator/AOT test harness, sibling repo)

Every sample is **offline** (no API keys) via `ANcpLua.Agents.Testing.FakeChatClient`, so it builds and runs in CI.

## Constraints discovered (and honored)

- Qyl **AutoInstrumentation** suite is **not on nuget.org** → showcased as a documented pattern in `samples/README.md`, not a live dependency.
- `Microsoft.Agents.AI.DevUI` / `.Hosting` are preview, `.Hosting.OpenAI` is alpha → sample-only pins (never packed); closure rests on the stable 1.11.0 core.
- `Microsoft.Agents.AI.Workflows.Generators` is an analyzer-only package (`IncludeBuildOutput=false`) → the generator-testing sample loads its assembly via `GeneratePathProperty` to obtain the public `ExecutorRouteGenerator` type.
- All samples: `Sdk="ANcpLua.NET.Sdk"`, `IsPackable=false`, central TFM/CPM, registered in `ANcpLua.Agents.slnx`.

## Samples (each = one distinct combination)

| Project | Exercises |
|---|---|
| `AgentBuilder.Fluent` | `QylAgentOptionsBuilder` → `ChatClientAgent` |
| `AgentTools.Governed` | `QylToolSet.From` + `UseQylGovernance` (`AgentBudgetEnforcer`/`AgentConcurrencyLimiter`/`AgentCapabilityContext`) |
| `AgentApproval.Gate` | `QylApprovalGate.RequireQylApproval` / `UseQylApproval` |
| `AgentStructuredOutput` | `RunQylWithSchemaAsync<T>` |
| `AgentConditionalTools` | `WithQylConditionalTools` (`AIContextProvider`) |
| `AgentGovernance.Lineage` | `AgentCallLineage` + `AgentSpawnTracker` + `AgentCallGuard` |
| `AgentTelemetry.SemConv` | `.Instrumentation` + Qyl SemanticConventions/Incubating (`gen_ai.*`) + OTLP |
| `AgentApiContracts` | `RunQylWithSchemaAsync<T>` producing `Qyl.Api.Contracts` DTOs |
| `AgentServiceDefaults.Web` | `AddQylAgentServiceDefaults` + `MapQylAgentEndpoints` (ASP.NET) |
| `AgentDevUI.Governed` | DevUI + OpenAI endpoints + governance + telemetry middleware |
| `AgentWorkflow.Chain` | `AddQylChain` |
| `AgentWorkflow.Switch` | `AddQylSwitch` |
| `AgentWorkflow.HumanInLoop` | `AddQylHumanInTheLoop` + checkpoint/resume |
| `AgentWorkflow.Declarative` | `QylDeclarativeAgent.Build` (YAML) |
| `AgentTesting.Harness` (test) | `.Testing` (`AgentRunHarness`, `ActivityAssert`) + `.Testing.Workflows` (`WorkflowFixture<T>`) |
| `AgentWorkflow.Generators.Tested` (test) | MAF `ExecutorRouteGenerator` via ANcpLua `Test<TGenerator>` → `.Compiles().HasNoForbiddenTypes().IsCached().Produces()` + `Compile` + `TrimAssert`/`AotRuntime` |

Plus `samples/README.md` (full combination matrix + documented AutoInstrumentation/Aspire patterns) and `docs/showcase/maf-nuget-channel-matrix.md` (the 34-package channel matrix incl. `Declarative @ 1.11.0-rc1`).

## Verification

`dotnet build ANcpLua.Agents.slnx` (all green) + `dotnet test` for the two test projects. Then push `feat/combination-showcase` and open a PR.

## API authority

MAF calls grep-verified against `~/RiderProjects/qyl-workspace/agent-framework-dotnet-rootsource` @ dotnet-1.11.0 (rename rules: `AgentSession`/`AgentResponse`/`CreateSessionAsync`/`RunAsync`, never `AgentThread`/`CompleteAsync`). ANcpLua.Agents calls verified against the repo's `src/` public surface. Existing `samples/AgentTelemetry.Minimal` + `samples/AgentDevUI` are the canonical patterns.
