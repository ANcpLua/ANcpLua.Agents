# ANcpLua.Agents — Showcase Spec

Status: **draft for approval** (2026-06-25). Supersedes the 14-runnable-app draft per review.
Build does not start until the Open Decisions below are closed.

## Principle

Foreground the genuine IP (**governance**). Be honest where MAF-native already suffices.
**Enrich, don't recreate.** A sample that wraps what MAF gives you for free is a liability, not a feature.

## Runnable samples (the set a newcomer copies) — target 6

| # | Project | Proves | Key types |
|---|---------|--------|-----------|
| 1 | `AgentGovernance` | The moat: capability gate + per-tool budget reserve/rollback + concurrency, composed in front of a tool | `GovernedAIFunction`, `AgentCapabilityContext`, `AgentBudgetEnforcer`, `AgentCallLineage` |
| 2 | `AgentApproval` (rebuilt #3) | Deterministic policy-denial **vs** human-in-the-loop — when to use which | `UseQylApproval` (throw-on-denial) beside MAF-native `ApprovalRequiredAIFunction` + `ToolApprovalRequestContent` |
| 3 | `AgentTelemetry.SemConv` (rebuilt #7) | Enrich, don't recreate — MAF-native spans + qyl enrichment | `UseOpenTelemetry()` (→ `invoke_agent`/`execute_tool`) + qyl `SemanticConventions.Incubating` `gen_ai.*` + one custom `ActivitySource` deep-eval span |
| 4 | `AgentServiceDefaults` | Health endpoints + MAF `ActivitySource` registration | `ANcpLua.Agents.Hosting.ServiceDefaults` |
| 5 | `AgentWorkflow.Declarative` | Stable declarative workflow surface | `ANcpLua.Agents.Workflows.Declarative` |
| 6 | `AgentDevUI` (existing, green) | Governed/telemetered agent surfaced **through** DevUI | `AddAIAgent` + `MapDevUI`, telemetry via `UseAgentTelemetry` |

All samples: `IsPackable=false`, in the `/samples/` slnx folder, offline via `FakeChatClient` (no keys, CI-safe).

## Asserted matrix (harness rows, NOT runnable apps)

The full combinator lattice lives in the **test harness** as `FakeChatClient` + `ActivityListener`
assertions (`ANcpLua.Agents.Testing.Workflows`). Coverage up, sprawl down, CI can't let it rot.
README line: **"runnable: these 6 · asserted: all."**

Rows to assert (named from the real workflow/agent surface):
- **Composition** — `Chain` ∪ `Switch` collapsed to one `AgentWorkflowBuilder` combinator row.
- **Ergonomics trio** — `ChatClientAgent + {options-builder, schema, background}` — three rows, co-located (this was draft samples 1/4/5; it's one agent + one extension, ×3).
- **Workflow execution** — `InProcessExecution`, checkpoint/resume, fan-in.
- **Governance combinations** — capability-deny, budget-rollback-on-throw, concurrency-cap (asserted, since the runnable #1 only demos the happy + one deny path).

## Rebuilt sample designs

### #2 `AgentApproval` — policy-denial vs HITL
- **Drop** `QylApprovalGate` (ceremony over `ApprovalRequiredAIFunction`).
- Left panel: `UseQylApproval(predicate)` → deterministic throw on denial (single-call, no round-trip).
- Right panel: MAF-native `ApprovalRequiredAIFunction` → agent emits `ToolApprovalRequestContent`, caller approves/denies (HITL multi-turn).
- Teaching: deterministic guardrail vs human gate — different tools, not competitors.

### #3 `AgentTelemetry.SemConv` — enrich, don't recreate
- Framework spans: `agent.AsBuilder().UseOpenTelemetry()` → native `invoke_agent` + `execute_tool` (GenAI semconv, bounded by default).
- Enrichment: a custom `ActivitySource` emits **one** qyl deep-eval span, tagged with qyl
  `SemanticConventions.Incubating` `gen_ai.*` constants — makes the Incubating live-dep earn its place.
- **No** `UseAgentRunTelemetry`/`UseAgentToolTelemetry` (non-semconv `agent.*` — deleted, see prerequisite).

## Prerequisite (already concluded last turn)

Gut the hand-rolled telemetry decorators: delete `AgentRunTelemetryAgent`, `AgentToolTelemetryAgent`,
`AgentTelemetryInstrumentation`, `AgentTelemetryNames`, `AgentTelemetryOptions`.
`ANcpLua.Agents.Instrumentation` becomes thin registration helpers — `AddAgentFrameworkSources`/
`AddAgentFrameworkMeters` corrected to the real source `"Experimental.Microsoft.Agents.AI"`
(current `FrameworkActivitySourceNames` is drifted to `"Microsoft.Agents.AI"` and misses native spans).

## Harness capture — standardize on ONE

`ActivityListener` (System.Diagnostics, **in-box, no new dep**) is the single capture mechanism for
span assertions. `ActivityAssert` helper wraps it so every telemetry test reads identically.
(`AddInMemoryExporter` pulls `OpenTelemetry.Exporter.InMemory` — a new dep — so reserve it only if a
test must assert post-SDK export shape. Default: don't.)

## Committed nuget matrix doc

Date it (**"as of 2026-06-25"**) + pin to a `dotnet package search` snapshot at commit time.
`dotnet build` already verifies the pins, so no pre-verification — just no undated matrix.

## Open decisions (close these, then build)

1. **Long-tail enumeration.** I don't have the original 14. Approve "**6 runnable + lattice asserted**"
   (rows above), or paste the 14 so I map each to runnable-or-asserted.
2. **Telemetry package shape.** Keep the thin `UseAgentTelemetry` convenience shim, or have samples call
   `UseOpenTelemetry()` directly and reduce `.Instrumentation` to registration-only? **Rec: keep the shim**
   (one-call bounded default), samples still show raw `UseOpenTelemetry()` to teach the native path.
3. **`SemanticConventions.Incubating` surface.** Sample #3 grounds against the qyl SemConv package's actual
   `gen_ai.*` constants at build time (cross-repo live dep) — confirm that's consumable here.

## Build order (on approval)

telemetry gut → 6 runnable projects → harness asserted rows → samples README + dated nuget matrix → `dotnet build` + 148-test green.
