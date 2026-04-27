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
- **MAF version discipline**: bump `Microsoft.Agents.AI.*` in `Version.props` as a group. The numeric base ships in lock-step from upstream (currently `1.3.0`), but the family spans mixed release tracks — stable for `core`/`Workflows`, `-preview` for `Hosting`/`AGUI.AspNetCore`/`Anthropic`, `-rc1` for `Workflows.Declarative`. Mismatched numeric bases break assembly binding; mismatched tracks within the same numeric base are what upstream actually ships, so keep them aligned but accept the suffix variance.

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
