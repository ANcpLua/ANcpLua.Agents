# AGENTS.md — ANcpLua.Agents

Instructions for AI coding agents working in this repo.
## Framework conventions

Branch protection, auto-merge, CodeRabbit posture, release flow, dependency
graph, and the cross-repo bootstrap rules for the four ANcpLua framework
repos are documented in one place at
[ANcpLua/renovate-config](https://github.com/ANcpLua/renovate-config#ancplua-framework-conventions--renovate-config).
This file documents conventions specific to this repo only.


## What this repo is

MAF-specific runtime helpers, instrumentation middleware, workflow helpers, service defaults, and test infrastructure. Split out of `ANcpLua.Roslyn.Utilities` so Roslyn-only consumers (analyzers, generators, foundation libs) don't pull MAF transitively.

## Package layout



```
src/
  ANcpLua.Agents/                   # Runtime helpers + governance primitives
    Governance/                     # AgentCallLineage, AgentCallGuard, AgentSpawnTracker,
                                    # AgentBudgetEnforcer, AgentConcurrencyLimiter,
                                    # AgentCapabilityContext, GovernedAIFunction, AgentToolPolicy
    AgentsHelper, ColorHelper
  ANcpLua.Agents.Instrumentation/   # AddAgentTelemetry, AddAgentFrameworkSources/Meters,
                                    # UseAgentRunTelemetry, UseAgentToolTelemetry
  ANcpLua.Agents.Hosting.ServiceDefaults/ # Health endpoints + MAF ActivitySource helpers
  ANcpLua.Agents.Workflows/         # Workflow facades and execution helpers
  ANcpLua.Agents.Workflows.Declarative/ # Stable declarative workflow helpers
  ANcpLua.Agents.Testing/           # FakeChatClient, fixtures, conformance suites
  ANcpLua.Agents.Testing.Workflows/ # WorkflowFixture, WorkflowHarness
tests/
  ANcpLua.Agents.Tests/             # Unit tests for governance, instrumentation, workflows, package boundaries
samples/
  AgentTelemetry.Minimal/           # Local fake-agent telemetry smoke; no live API key
```

### Module guide

- **Governance** — bounded-autonomy primitives. `AgentCallLineage` enforces depth + spawn budgets via `AsyncLocal`. `AgentBudgetEnforcer` tracks per-tool attempts and tool-call counts with rollback-on-failure reservations. `AgentConcurrencyLimiter` is a per-tool semaphore. `GovernedAIFunction` composes capability check + budget reservation + concurrency slot in front of any `AIFunction`. Producers project their richer descriptors down to the minimal `AgentToolPolicy` record. Human approval is orthogonal — wrap tools in `ApprovalRequiredAIFunction` from `Microsoft.Agents.AI` to drive the native `ToolApprovalRequestContent` loop.
- **Instrumentation** — separate package. `AddAgentTelemetry`, `AddAgentFrameworkSources`, `AddAgentFrameworkMeters`, `UseAgentRunTelemetry`, and `UseAgentToolTelemetry` instrument MAF middleware with bounded spans and metrics. Never reintroduce raw-argument/result logging or legacy Qyl-branded telemetry decorators.
- **Provider facades** — removed. Do not add compatibility shims for deleted hosting facades, MCP wrappers, data-ingestion helpers, Durable generator experiments, or product-host samples. Declarative workflows may exist only as a stable workflow package, not as a provider facade.

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

- **Runtime vs instrumentation vs test split**: `ANcpLua.Agents` is the runtime/governance package, `ANcpLua.Agents.Instrumentation` is telemetry middleware, and `ANcpLua.Agents.Testing` adds fakes + fixtures. Keep them separate.
- **No dogfooded analyzers on the test package**: `ANcpLua.Agents.Testing` relaxes rules that don't fit test-double patterns.
- **MAF version discipline**: bump the remaining stable `Microsoft.Agents.AI.*` pins in `Version.props` as a group. The active toolkit is on the stable 1.9.0 line: `Microsoft.Agents.AI`, `Microsoft.Agents.AI.Abstractions`, `Microsoft.Agents.AI.Workflows`, and `Microsoft.Agents.AI.Workflows.Declarative`.

## Cross-Repo Awareness — was passiert, wenn du Versionen anfasst

Diese vier Repos bilden eine Bootstrap-Kette: `Roslyn.Utilities → NET.Sdk → (Analyzers, Agents)`. Truth-Source für Paket-Versionen ist **`ANcpLua.NET.Sdk/src/Build/Common/Version.props`**, in den SDK-NuGet-Packages gepackt und in jedes Consumer-Projekt geladen. Dein lokales `Version.props` (sofern vorhanden) wird *nach* der SDK-Datei importiert (last-wins) — gedacht, um lokal AHEAD der gerade-publizierten SDK zu pinnen.

Bevor du eine Variable in Truth oder im lokalen Override bumpst:

- **Truth fließt durch GlobalPackageReference.** Pakete wie `ANcpLua.Analyzers` werden von der SDK in *jedes* Consumer-Projekt injiziert. Wenn Truth auf eine Version zeigt, die noch nicht auf nuget.org liegt, scheitert jeder Restore mit `NU1102` — auch die SDK-eigenen Tests (sie packen ein Sample.csproj und builden es). Saubere Reihenfolge: zuerst das ausgeschriebene Repo taggen + auf NuGet bringen, dann Truth nachziehen.

- **Self-Reference: die eigene Paket-Version zeigt auf last-PUBLISHED.** Wenn ein lokales `Version.props` eine Variable für das *eigene* Paket des Repos hat (z.B. `ANcpLuaAnalyzersVersion` in `ANcpLua.Analyzers/Version.props`), muss sie auf die zuletzt-publizierte Version zeigen, nicht auf die hochzukommende. csproj/Tests-Files referenzieren das Paket via `PackageReference` und ziehen es beim Restore aus NuGet; während Restore (vor Pack) gibt's die hochzukommende Version noch nicht. CI stampt die neue Version per `-p:Version=X.Y.Z` erst zur Pack-Time.

- **Bumps haben transitive Konsequenzen unter CPM.** Z.B. `Meziantou.Framework.DependencyScanning 2.0.11` zieht `YamlDotNet ≥ 17.0.1`. Bei `ManagePackageVersionsCentrally=true` ist Downgrade ein Hard-Error (`NU1109`). Wenn ein Bump nicht greift, steht der Grund in der Restore-Fehlermeldung — vor dem nächsten Versuch lesen.

- **Lokales Override gleich/unter Truth ist Müll.** Gleich = Doppelpflege, unter = stille Regression. Pruning sinnvoll, sobald die SDK mit matching Werten publisht.

- **Publish triggert auf Tag-Push `v*`, gegated durch Tests.** Ein Tag auf einen build-broken Commit publisht nicht, bleibt aber als Ghost-Tag remote. Statt remote zu re-assignen (≈ Force-Push), nächste Patch-Version verwenden.

- **Verifiziere Versionen vor dem Bump.** Ein Tippfehler (`2.0.20` statt `2.0.11`) bricht die Topo-Kette, weil Truth in alle Konsumenten fließt. NuGet-API: `https://api.nuget.org/v3-flatcontainer/<lowercased-id>/index.json`.

## Related repos

- Foundation Roslyn helpers + netstandard2.0 lib: `ANcpLua/ANcpLua.Roslyn.Utilities`
- MSBuild SDK + build conventions: `ANcpLua/ANcpLua.NET.Sdk`
- Analyzers: `ANcpLua/ANcpLua.Analyzers`

## Scope of "outstanding"

"Pre-existing" is not an exclusion. Bugs surfaced during a session are in
scope — fix them, or list them with an explicit decision (won't fix
because X), not as "not introduced by me."

Outstanding items require external blockers. An item may remain outstanding
only if it has:
(a) an upstream PR/issue link, or
(b) a ship-date dependency on external code, or
(c) a cross-repo handoff with linked tracking, or
(d) an explicit won't-do with technical rationale (analyzer collision,
    ALC mismatch, protocol violation — not "not worth it").

"Future session" / "out of scope" / "not worth the rework" do not qualify.

## First-party dependencies

If a blocking issue is in a library you own (any repo under your GitHub
namespace), default to fixing it directly: patch → test → release → consume.
Do not file issues against your own repos as a workaround for fixing them.
External tracker (a) under "Scope of outstanding" applies only to
genuinely-external upstreams.
