Decision: Option A — merge ANcpLua.Agents → MAF.Advanced.Patterns (as named sub-packages)                          
                                     
 Context

 services/qyl.loom/Program.cs is 262 LOC. ~70 LOC of it is pure DI/MCP boilerplate; another 101 LOC is endpoint
 registration that belongs in its own file. services/qyl.loom/Agents/QylLoomAgentsBuilder.cs carries 66 LOC of
 .AsBuilder().UseQylAgentTelemetry().Build().RecordInQylInventory() repetition across 13 factories.
 services/qyl.loom.patterns/Agents/QylLoomPatternsAgentsBuilder.cs repeats the same shape. ~70 wrapping sites exist
 across the qyl tree.

 Three repos own pieces of the solution:
 - ANcpLua.Agents (/Users/ancplua/framework/ANcpLua.Agents) — published to nuget.org under three nupkg ids; zero
 external consumers; all four <PackageReference> consumers live inside qyl.
 - MAF.Advanced.Patterns (/Users/ancplua/framework/MAF.Advanced.Patterns) — pre-publish; zero external consumers;
 zero test infrastructure; zero analyzers; explicit charter (README.md:5) reads verbatim "Zero copy-pasted Microsoft
 source, zero business logic, zero re-implementation — bundling is the product."
 - qyl (/Users/ancplua/qyl) — domain consumer; the verbose composition lives here.

 Goal: ultra-facades collapse qyl composition to ~30 lines; one canonical MAF-consumer toolkit replaces two sibling
 repos with overlapping audiences.

 Why A and not B/C/D:
 - B (vendor ANcpLua.Agents into qyl) retires three published nupkgs and folds a generic MAF product into a
 qyl-specific tree. The conformance harness (6 provider fixtures across ANcpLua.Agents.Testing/) is genuinely
 reusable for any future MAF consumer; vendoring kills that asset.
 - C (everything into qyl) kills both reuse stories and turns MAF.Advanced.Patterns' showcase into a qyl-internal
 sample — the formal PRD #1 dies.
 - D (status quo + tighten) doesn't compress qyl composition unless ultra-facades are extracted, which itself shifts
 capability ownership — i.e. the structural move is necessary, just under a different name. The user's prompt
 explicitly rejects D as not solving the verbose-composition goal.
 - A (merge into MAF.Advanced.Patterns) is the only option where: (1) qyl ends up with one upstream MAF-consumer
 dependency to manage, (2) the conformance harness keeps its public surface, (3) sub-package split lets each charter
 survive intact, (4) the user's CLAUDE.md migration triage already lists move-to-MAF.Advanced.Patterns as a discrete
 option but no move-to-ANcpLua.Agents — direction was already implicit.

 Sub-package layout after merge (the how of A)

 The /Users/ancplua/framework/MAF.Advanced.Patterns solution post-migration:

 ┌─────────────────────────────────────────┬─────────────────────────┬──────────────────────────────────────────┐
 │                 Package                 │         Charter         │              Source content              │
 ├─────────────────────────────────────────┼─────────────────────────┼──────────────────────────────────────────┤
 │                                         │ Zero business logic,    │ unchanged + new                          │
 │ MAF.Advanced.Patterns (core)            │ facade bundling —       │ QylAgentExtensions.AsQylAgent            │
 │                                         │ charter unchanged       │                                          │
 ├─────────────────────────────────────────┼─────────────────────────┼──────────────────────────────────────────┤
 │                                         │ Runtime engines for MAF │ from ANcpLua.Agents/Governance/ (8       │
 │ MAF.Advanced.Patterns.Governance (NEW)  │  consumers (budget /    │ files) + /Factory/ (1 file) +            │
 │                                         │ lineage / concurrency / │ /Instrumentation/ (2 files)              │
 │                                         │  spawn / capability)    │                                          │
 ├─────────────────────────────────────────┼─────────────────────────┼──────────────────────────────────────────┤
 │                                         │ Conformance harness,    │ from ANcpLua.Agents.Testing/ (~4,500     │
 │ MAF.Advanced.Patterns.Testing (NEW)     │ FakeChatClient, 6       │ LOC)                                     │
 │                                         │ provider fixtures       │                                          │
 ├─────────────────────────────────────────┼─────────────────────────┼──────────────────────────────────────────┤
 │ MAF.Advanced.Patterns.Testing.Workflows │ Workflow test harness,  │ from ANcpLua.Agents.Testing.Workflows/   │
 │  (NEW)                                  │ sample workflows 01–07  │ (~3,500 LOC)                             │
 ├─────────────────────────────────────────┼─────────────────────────┼──────────────────────────────────────────┤
 │ .Azure / .Foundry / .Foundry.Hosting /  │ unchanged               │ unchanged                                │
 │ .OpenAI                                 │                         │                                          │
 └─────────────────────────────────────────┴─────────────────────────┴──────────────────────────────────────────┘

 Each sub-package ships its own <PackageReadmeFile> so charters don't bleed across packages on nuget.org.

 ---
 1. Capability → final home table

 Capability: Provider→agent adapters (low layer: client factory)
 Today: ANcpLua.Agents.Factory.AgentChatClientFactory
 After Option A: MAF.Advanced.Patterns.Governance (kept as-is)
 ────────────────────────────────────────
 Capability: Provider→agent adapters (high layer: facade)
 Today: MAF.Advanced.Patterns.Qyl{OpenAI,Anthropic,GitHubCopilot,CopilotStudio}ClientExtensions
 After Option A: unchanged in MAF.Advanced.Patterns core
 ────────────────────────────────────────
 Capability: Agent governance (budget / lineage / concurrency / spawn / tool-policy)
 Today: ANcpLua.Agents/Governance/ (8 files, ~600 LOC)
 After Option A: MAF.Advanced.Patterns.Governance
 ────────────────────────────────────────
 Capability: Telemetry decorators (TracedAIFunction, ToolDecoratingChatClient)
 Today: ANcpLua.Agents/Instrumentation/
 After Option A: MAF.Advanced.Patterns.Governance
 ────────────────────────────────────────
 Capability: WithQylTelemetry / UseQylTelemetry / UseQylAgentTelemetry
 Today: qyl internal/qyl.instrumentation/.../GenAiInstrumentation.cs:53,100,141
 After Option A: stays in qyl — depends on Qyl.OpenTelemetry.SemanticConventions.Incubating and
   ActivitySources.GenAiSource; both qyl-internal and not portable.
 ────────────────────────────────────────
 Capability: UseQylMcpInstrumentation (MCP transport facade)
 Today: qyl internal/qyl.instrumentation/.../QylMcpServerInstrumentation.cs
 After Option A: stays in qyl — qyl JSON-RPC envelope spans + qyl-shape mcp.transport/mcp.session.id (PR #172
   invariant)
 ────────────────────────────────────────
 Capability: Workflow facades (QylWorkflow*Extensions ×7)
 Today: MAF.Advanced.Patterns core
 After Option A: unchanged
 ────────────────────────────────────────
 Capability: MCP server/client facades (QylMcpExtensions, QylDeclarativeMcpExtensions, QylMcpToolHandler)
 Today: MAF.Advanced.Patterns core
 After Option A: unchanged + new AddQylMcpServer overload that takes a UseQylMcpInstrumentation-shaped callback
 ────────────────────────────────────────
 Capability: Three-builder shape (chat→agents→workflow)
 Today: qyl services/qyl.loom*/Agents/
 After Option A: stays in qyl — domain-specific DI ergonomics. New AsQylAgent from MAF.Advanced.Patterns shrinks each

   factory body to ~3 LOC.
 ────────────────────────────────────────
 Capability: Testing harness (FakeChatClient, conformance, 6 provider fixtures)
 Today: ANcpLua.Agents.Testing
 After Option A: MAF.Advanced.Patterns.Testing
 ────────────────────────────────────────
 Capability: Workflow testing harness (sample workflows 01–07)
 Today: ANcpLua.Agents.Testing.Workflows
 After Option A: MAF.Advanced.Patterns.Testing.Workflows
 ────────────────────────────────────────
 Capability: Live LLM smoke (Tests.Live/LiveChatClientSmokeTests, LiveModelCapabilityDenialTests,
   LiveModelToolBudgetTests)
 Today: ANcpLua.Agents/Tests.Live/
 After Option A: MAF.Advanced.Patterns.Testing/Live/ (env vars rename: ANCPLUA_AGENT_* → MAF_AGENT_*)
 ────────────────────────────────────────
 Capability: InvestigationLineage (qyl wrapper over AgentCallLineage)
 Today: qyl services/qyl.mcp/Agents/InvestigationLineage.cs
 After Option A: stays in qyl — hardcodes QYL_AGENT_MAX_DEPTH / QYL_AGENT_MAX_SPAWNS env names; not generalizable.
 ────────────────────────────────────────
 Capability: Roslyn analyzer QYL0135
 Today: qyl internal/qyl.instrumentation.generators/Analyzers/AgentCompositionRootAnalyzer.cs
 After Option A: stays in qyl — pins to qyl's GenAiInstrumentation metadata name.
 ────────────────────────────────────────
 Capability: qyl-domain DI bundle (autofix + exploration + code-review wiring)
 Today: scattered in qyl.loom/Program.cs:47-76
 After Option A: new qyl-internal services/qyl.loom/Hosting/QylLoomDefaults.cs (AddQylLoomDefaults)
 ────────────────────────────────────────
 Capability: qyl-domain endpoint registration
 Today: inline in qyl.loom/Program.cs:114-217
 After Option A: new qyl-internal services/qyl.loom/Endpoints/QylLoomEndpoints.cs (MapQylLoomEndpoints)

 ---
 2. Deletion list

 Disappears completely (3 published nupkgs, 1 repo):
 - /Users/ancplua/framework/ANcpLua.Agents/ — entire repo, after content migrates. Archive on GitHub; do not delete.
 - nuget.org/packages/ANcpLua.Agents — deprecate with <PackageReadmeFile> redirect notice →
 MAF.Advanced.Patterns.Governance.
 - nuget.org/packages/ANcpLua.Agents.Testing — deprecate → MAF.Advanced.Patterns.Testing.
 - nuget.org/packages/ANcpLua.Agents.Testing.Workflows — deprecate → MAF.Advanced.Patterns.Testing.Workflows.

 Disappears from qyl (boilerplate collapse):
 - services/qyl.loom/Program.cs:47-76 (~49 LOC of DI registration) → builder.AddQylLoomDefaults() (1 line).
 - services/qyl.loom/Program.cs:90-110 (~21 LOC of MCP wiring) → services.AddQylMcpServer(transport, instrumentation)
  (4 lines).
 - services/qyl.loom/Program.cs:114-217 (~101 LOC of endpoints) → moved as-is to
 services/qyl.loom/Endpoints/QylLoomEndpoints.cs, registered via app.MapQylLoomEndpoints() (1 line in Program.cs).
 - services/qyl.loom/Agents/QylLoomAgentsBuilder.cs — Compose() helper (lines 117-132) + per-factory wrap repeats
 (~52 LOC). Each factory body becomes clients.BuildChatClient("rca").AsQylAgent(name, desc, instr, b =>
 b.UseQylAgentTelemetry(), services).RecordInQylInventory(...).
 - services/qyl.loom.patterns/Agents/QylLoomPatternsAgentsBuilder.cs — same shape, ~33 LOC removed.
 - services/qyl.mcp/Hosting/QylMcpServerRegistration.cs:49-85 (~36 LOC of task store + transport + instrumentation
 chaining) → services.AddQylMcpServer(...) (3 LOC). Lines 86-121 (admin denial / scope injection / response shaping
 filters) stay — those are business logic.

 Stays untouched in qyl:
 - All autofix orchestration (LoomAutofixRunner.cs:88-269 — 180 LOC of real logic, not boilerplate).
 - All exploration orchestration (ExplorationOrchestrator.cs).
 - internal/qyl.instrumentation/ and internal/qyl.instrumentation.generators/.
 - services/qyl.mcp/Agents/InvestigationLineage.cs.

 ---
 3. Analyzer relocation

 internal/qyl.instrumentation.generators/Analyzers/AgentCompositionRootAnalyzer.cs:49 does NOT change.

 private const string QylTelemetryExtensionsMetadataName =
     "Qyl.Instrumentation.Instrumentation.GenAi.GenAiInstrumentation";

 UseQylAgentTelemetry stays in qyl because its 3-line body (GenAiInstrumentation.cs:141-146) calls
 .UseOpenTelemetry(sourceName) with default "qyl.agent" and uses Qyl.OpenTelemetry.SemanticConventions.Incubating
 types — none of which are portable to MAF.Advanced.Patterns without dragging the qyl semconv package along.

 The new AsQylAgent facade in MAF.Advanced.Patterns accepts a telemetry callback so the call chain at qyl call sites
 still passes through UseQylAgentTelemetry:

 // MAF.Advanced.Patterns/QylAgentExtensions.cs (NEW)
 public static AIAgent AsQylAgent(
     this IChatClient client,
     string name,
     string description,
     string instructions,
     Action<AIAgentBuilder>? telemetry = null,
     IServiceProvider? services = null)
 {
     Guard.NotNull(client);
     var options = new ChatClientAgentOptions
     {
         Name = name,
         Description = description,
         ChatOptions = new() { Instructions = instructions }
     };
     var builder = client.AsAIAgent(options).AsBuilder();
     telemetry?.Invoke(builder);
     return services is null ? builder.Build() : builder.Build(services);
 }

 QYL0135's symbol-walking logic (lines 234-258 of the analyzer) already inspects the full agent-construction call
 chain. After migration, qyl call sites read:

 clients.BuildChatClient("rca")
     .AsQylAgent(name, desc, instr, b => b.UseQylAgentTelemetry(), services)
     .RecordInQylInventory(...);

 UseQylAgentTelemetry still appears as a syntactic node in the chain → analyzer still binds → QYL0135 still fires
 when omitted.

 Required regression test (path): tests/qyl.instrumentation.generators.tests/AgentCompositionRootAnalyzerTests.cs —
 add a case where AsQylAgent is called WITHOUT the telemetry callback. Expected: QYL0135 fires.

 ---
 4. Two biggest sequencing risks (concrete)

 Risk 1 — qyl PackageReference flip races nupkg publish

 qyl's Directory.Packages.props currently pins ANcpLua.Agents and ANcpLua.Agents.Testing from nuget.org. The
 post-migration pin is MAF.Advanced.Patterns.Governance and MAF.Advanced.Patterns.Testing. If qyl's CPM file flips
 before the new packages exist on nuget.org, qyl CI breaks immediately and there's no rollback short of reverting the
  CPM commit.

 Mitigation — three-step PR sequence:

 1. PR-A in MAF.Advanced.Patterns: create the four sub-package csprojs with content copied from ANcpLua.Agents. Push
 to nuget.org with stamp 1.3.0-preview-consolidated.260429.1. Smoke install in dotnet new web --output /tmp/scratch
 before any qyl-side change.
 2. PR-B in qyl: flip Directory.Packages.props, update using ANcpLua.Agents.Governance; → using
 MAF.Advanced.Patterns.Governance; (~70 sites, IDE-driven), add the new ultra-facade call sites. CI gate: nuke Full
 must report zero errors.
 3. PR-C in ANcpLua.Agents: deprecate the three nupkgs on nuget.org with redirect README. Archive the GitHub repo.

 Risk 2 — README charter pollution baked into nupkg metadata

 MAF.Advanced.Patterns README.md:5 says verbatim "Zero copy-pasted Microsoft source, zero business logic, zero
 re-implementation — bundling is the product." Governance engines (real budget/lineage/concurrency runtime logic)
 violate that statement. nuget.org caches package metadata immutably per version — if the first nupkg containing
 MAF.Advanced.Patterns.Governance ships with the bundling-only charter as its <PackageReadmeFile>, that wrong charter
  is permanently archived for that version.

 Mitigation: in PR-A, the FIRST file change is README split. Each sub-project ships its own README via
 <PackageReadmeFile> in its csproj:
 - src/MAF.Advanced.Patterns/README.md — bundling-only charter (unchanged sentence).
 - MAF.Advanced.Patterns.Governance/README.md — runtime-engines charter (NEW).
 - MAF.Advanced.Patterns.Testing/README.md — conformance-harness charter (NEW).
 - MAF.Advanced.Patterns.Testing.Workflows/README.md — workflow-harness charter (NEW).

 CI gate (PR-A): a Roslyn check that fails the build if any Governance/-namespaced type lives in a project whose
 <PackageReadmeFile> is the bundling-only README.

 ---
 5. Effort estimate

 ┌─────────────────────────────────────┬─────────────────────────────────────────────────────────────────────────┐
 │                Item                 │                                  Count                                  │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │ LOC moved (ANcpLua.Agents →         │ ~8,500 (Governance ~600 + Factory ~60 + Instrumentation ~150 + Testing  │
 │ MAF.Advanced.Patterns)              │ ~4,500 + Testing.Workflows ~3,500)                                      │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │ LOC eliminated from qyl             │ ~150 (Program.cs DI 49 + MCP wiring 21 + agent factory boilerplate 66 + │
 │                                     │  MCP server registration 36 − new ultra-facade call sites ~22)          │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │ LOC moved within qyl (Program.cs →  │ ~101 (cut/paste, no logic change)                                       │
 │ Endpoints/QylLoomEndpoints.cs)      │                                                                         │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │ New csproj created                  │ 3 (MAF.Advanced.Patterns.Governance, .Testing, .Testing.Workflows)      │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │ Csproj deleted                      │ 3 (ANcpLua.Agents, .Testing, .Testing.Workflows)                        │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │ qyl <PackageReference> touched      │ 4 (qyl.collector.tests, qyl.instrumentation, qyl.loom.patterns,         │
 │                                     │ qyl.mcp)                                                                │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │                                     │ 3 (MAF.Advanced.Patterns.QylAgentExtensions.AsQylAgent,                 │
 │ New ultra-facades to author         │ QylMcpExtensions.AddQylMcpServer overload, qyl-internal                 │
 │                                     │ Qyl.Loom.Hosting.QylLoomDefaults.AddQylLoomDefaults)                    │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │ qyl call sites changed              │ ~70 wrapping sites (using-statement updates + AsQylAgent switchover);   │
 │                                     │ IDE-driven                                                              │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │ QYL0135 analyzer code changes       │ 0 LOC (regression test added in                                         │
 │                                     │ tests/qyl.instrumentation.generators.tests/)                            │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │ nupkg deprecations to file          │ 3 (on nuget.org with redirect README)                                   │
 ├─────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────┤
 │                                     │ 3-4 working days (1.5 days move + 0.5 day ultra-facade authoring + 0.5  │
 │ Estimated solo-dev wall-clock       │ day qyl call-site sweep + 0.5 day nuke Full + nupkg publish + 0.5 day   │
 │                                     │ deprecation ceremony)                                                   │
 └─────────────────────────────────────┴─────────────────────────────────────────────────────────────────────────┘

 ---
 6. The new qyl.loom/Program.cs sketch

 using MAF.Advanced.Patterns;
 using Qyl.Instrumentation.Instrumentation;
 using Qyl.Loom.Endpoints;
 using Qyl.Loom.Hosting;

 var builder = WebApplication.CreateBuilder(args);
 builder.Host.UseQylListenPort();                    // MAF.Advanced.Patterns — Railway $PORT
 builder.AddQylServiceDefaults();                    // qyl.instrumentation
 builder.AddQylLoomDefaults();                       // NEW qyl-internal: replaces 49 LOC of DI

 builder.Services
     .AddQylMcpServer(                               // MAF.Advanced.Patterns: replaces 21 LOC
         transport: McpTransport.Http,
         instrumentation: opts => opts.Transport = "http")
     .WithTools<LoomGodAnalyzerServer>()
     .WithTools<LoomWorkflowTools>()
     .WithPrompts<LoomPrompts>();

 var app = builder.Build();
 app.MapQylLoomEndpoints();                          // NEW qyl-internal: replaces 101 LOC

 app.Run();

 ~17 LOC, down from 262. Each remaining line earns its keep: hosting defaults, MCP transport choice, tool/prompt
 registration (qyl-domain), endpoint registration. Zero hand-rolled DI repetition.

 ---
 Verification

 1. MAF.Advanced.Patterns build: dotnet build MAF.Advanced.Patterns.slnx --nologo /clp:ErrorsOnly — 0 errors across
 all 8 projects.
 2. Pre-publish smoke: dotnet add /tmp/scratch package MAF.Advanced.Patterns.Governance --source nuget.org
 --prerelease restores cleanly.
 3. qyl gate: nuke Full from /Users/ancplua/qyl — 0 errors, all xUnit MTP tests green, frontend build green, nuke
 Verify confirms generated artefacts unchanged.
 4. Composition surface check: wc -l services/qyl.loom/Program.cs ≤ 30; QylLoomAgentsBuilder.cs factory bodies ≤ 3
 LOC each (grep -A 3 "BuildXxxAgent" confirms).
 5. Analyzer regression: tests/qyl.instrumentation.generators.tests/AgentCompositionRootAnalyzerTests.cs — new test
 exercising AsQylAgent without telemetry callback must trigger QYL0135.
 6. Telemetry continuity (manual): nuke DockerUp, exercise an autofix run via MCP (qyl.generate_fix), verify in
 qyl-collector dashboard that gen_ai.execute_tool spans + JSON-RPC envelope spans still emit and mcp.transport=http
 rides on every span (PR #172 invariant).
 7. Conformance harness: from MAF.Advanced.Patterns, dotnet test MAF.Advanced.Patterns.Testing.Conformance.slnx — all
  6 provider fixtures green (or skipped on missing API keys).
 8. Live smoke (optional, manual): dotnet test --filter Category=Live with MAF_AGENT_API_KEY / MAF_AGENT_MODEL /
 MAF_AGENT_ENDPOINT set.

 ---
 Files to consult during implementation

 - /Users/ancplua/qyl/services/qyl.loom/Program.cs — collapse target (262 → ~17 LOC).
 - /Users/ancplua/qyl/services/qyl.loom/Agents/QylLoomAgentsBuilder.cs — 13 factory bodies to compress.
 - /Users/ancplua/qyl/services/qyl.loom.patterns/Agents/QylLoomPatternsAgentsBuilder.cs — 3 factory bodies to
 compress.
 - /Users/ancplua/qyl/services/qyl.mcp/Hosting/QylMcpServerRegistration.cs:49-85 — 36 LOC absorbed by
 AddQylMcpServer.
 - /Users/ancplua/qyl/internal/qyl.instrumentation/Instrumentation/GenAi/GenAiInstrumentation.cs:141 —
 UseQylAgentTelemetry, stays in qyl.
 - /Users/ancplua/qyl/internal/qyl.instrumentation.generators/Analyzers/AgentCompositionRootAnalyzer.cs:49 — QYL0135
 metadata name pin, no change.
 - /Users/ancplua/framework/ANcpLua.Agents/src/ANcpLua.Agents/Governance/ — 8 files, full move.
 - /Users/ancplua/framework/ANcpLua.Agents/src/ANcpLua.Agents/Factory/AgentChatClientFactory.cs — single file, full
 move.
 - /Users/ancplua/framework/ANcpLua.Agents/src/ANcpLua.Agents/Instrumentation/{ToolDecoratingChatClient,TracedAIFunct
 ion}.cs — full move.
 - /Users/ancplua/framework/MAF.Advanced.Patterns/README.md:5 — bundling-only charter sentence to retire (split into
 per-package READMEs).
 - /Users/ancplua/framework/MAF.Advanced.Patterns/src/MAF.Advanced.Patterns/QylMcpExtensions.cs — add
 AddQylMcpServer(transport, instrumentation) overload that takes a UseQylMcpInstrumentation-shaped callback.
 - /Users/ancplua/framework/MAF.Advanced.Patterns/Directory.Packages.props — add CPM pins for any new dependencies
 the Governance/Testing packages drag in.
 - /Users/ancplua/qyl/Directory.Packages.props — flip ANcpLua.Agents* →
 MAF.Advanced.Patterns.{Governance,Testing,Testing.Workflows}.
 - /Users/ancplua/qyl/CLAUDE.md — update capability map and "MAF.Advanced.Patterns — consume, don't duplicate"
 section after migration. Drop the "External consumers — None found" line; add governance + testing rows to the
 QylXxxExtensions table.