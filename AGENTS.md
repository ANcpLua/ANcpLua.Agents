# AGENTS.md — ANcpLua.Agents

Instructions for AI coding agents working in this repo.

## What this repo is

MAF-specific runtime helpers + test infrastructure. Split out of `ANcpLua.Roslyn.Utilities` so Roslyn-only consumers (analyzers, generators, foundation libs) don't pull MAF transitively.

## Package layout



```
src/
  ANcpLua.Agents/                   # Runtime helpers
    Governance/                     # AgentCallLineage, AgentCallGuard, AgentSpawnTracker,
                                    # AgentBudgetEnforcer, AgentConcurrencyLimiter,
                                    # AgentCapabilityContext, GovernedAIFunction, AgentToolPolicy
    Instrumentation/                # TracedAIFunction
    Factory/                        # AgentChatClientFactory (OpenAI-compatible)
    AgentsHelper, WorkflowsHelper, ColorHelper
  ANcpLua.Agents.Testing/           # FakeChatClient, fixtures, conformance suites
  ANcpLua.Agents.Testing.Workflows/ # WorkflowFixture, WorkflowHarness
tests/
  ANcpLua.Agents.Tests/             # Unit tests for Governance/Instrumentation/Factory
```

### Module guide

- **Governance** — bounded-autonomy primitives. `AgentCallLineage` enforces depth + spawn budgets via `AsyncLocal`. `AgentBudgetEnforcer` tracks per-tool attempts and tool-call counts with rollback-on-failure reservations. `AgentConcurrencyLimiter` is a per-tool semaphore. `GovernedAIFunction` composes capability check + budget reservation + concurrency slot in front of any `AIFunction`. Producers project their richer descriptors down to the minimal `AgentToolPolicy` record. Human approval is orthogonal — wrap tools in `ApprovalRequiredAIFunction` from `Microsoft.Agents.AI` to drive the native `ToolApprovalRequestContent` loop.
- **Instrumentation** — generic OTel tracing without semconv hardcoding. `TracedAIFunction` wraps an `AIFunction` and emits an `execute_tool` span with GenAI semconv 1.40 tags, plus optional caller-supplied tags. `ToolDecoratingChatClient` is a `DelegatingChatClient` that runs a caller-supplied `Func<AIFunction, AIFunction>` over every tool in `ChatOptions.Tools` — use it when the decoration must apply regardless of where tools were registered. Consumers bring their own telemetry extension (see qyl `UseQylTelemetry`).
- **Factory** — `AgentChatClientFactory.TryCreate(options)` builds an `IChatClient` over the OpenAI .NET SDK; supports any OpenAI-compatible endpoint (Ollama, Anthropic-via-proxy, Azure, …). `TryCreateFromEnvironment()` reads `ANCPLUA_AGENT_API_KEY` / `ANCPLUA_AGENT_MODEL` / `ANCPLUA_AGENT_ENDPOINT`.

All packages target `net10.0`. Foundation Roslyn helpers live in the sibling `ANcpLua.Roslyn.Utilities` repo and are consumed here via plain `<PackageReference>` pinned in `Directory.Packages.props`.

## Conventions

Imported from `ANcpLua.NET.Sdk` (global `AGENTS.md`). Key house rules:

- UTF-8 with BOM on new `.cs` files
- File-scoped namespaces, primary constructors, required init properties
- Switch expressions over if-else chains
- `TimeProvider.System` — never `DateTime.UtcNow`
- No suppression: no `#pragma warning disable`, no `[SuppressMessage]`, no `<NoWarn>` (exception: NU1604/NU1701 TFM-specific in `Directory.Build.props`)
- Async methods use `Async` suffix
- Tests: Arrange / Act / Assert comments, xUnit v3 + MTP

## Key design choices

- **Runtime vs test surface split**: `ANcpLua.Agents` is the runtime package (no test-only types). `ANcpLua.Agents.Testing` adds fakes + fixtures. Keep them separate — consumers should never need both in production.
- **No dogfooded analyzers on the test package**: `ANcpLua.Agents.Testing` relaxes rules that don't fit test-double patterns.
- **MAF version discipline**: bump `Microsoft.Agents.AI.*` in `Version.props` as a group. The whole family ships in lock-step from upstream; mixing versions breaks assembly binding.

## Related repos

- Foundation Roslyn helpers + netstandard2.0 lib: `ANcpLua/ANcpLua.Roslyn.Utilities`
- MSBuild SDK + build conventions: `ANcpLua/ANcpLua.NET.Sdk`
- Analyzers: `ANcpLua/ANcpLua.Analyzers`
