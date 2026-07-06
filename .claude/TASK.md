# Task: MAF 1.11.0 → 1.13.0 upgrade + version refresh

## End goal
Bump the repo to the MAF 1.13.0 stable line, adopt what 1.13 newly enables,
refresh first-party pins (Roslyn.Utilities 2.2.29, Qyl 3.1.0 / 0.2.2), and
reword stale absolutist comments ("never", "do NOT") into present-tense
statements. Public API of this repo's packages does not need to be preserved —
only Qyl consumes them.

## Facts verified on NuGet (2026-07-06)
- Microsoft.Agents.AI / .Abstractions / .Workflows / .Workflows.Declarative → **1.13.0** stable
- Microsoft.Agents.AI.Hosting / .DevUI → **1.13.0-preview.260703.1**; Hosting.OpenAI → **1.13.0-alpha.260703.1**
- Microsoft.Agents.AI.LocalCodeAct → **not published yet** (skip)
- ANcpLua.Roslyn.Utilities(.Testing/.Testing.Aot) → **2.2.29**
- Qyl.OpenTelemetry.SemanticConventions(+Incubating) → **3.1.0**; Qyl.Api.Contracts → **0.2.2**

## Breaking changes relevant to this repo (from the 1.10→1.13 changelog)
- #6667: OTel instrumentation repositioned below FunctionInvokingChatClient — tool-calling
  agents now emit `execute_tool` spans parented under `invoke_agent` (instrumentation tests/docs)
- #6636/#6670: checkpoint TypeId ignores assembly version — checkpoints survive package upgrades
- #6491/#6574: fan-in checkpoint state persisted correctly
- #6855 (OpenAI hosting 400s), #6521/#6729 (approval defaults), #6906 (ShellPolicy) — mostly
  hosting/harness surface; check samples (AgentDevUI, AgentApproval.Gate) for impact
- #6743: experimental id OPENAI001 → MAAI001 — repo bans suppressions, but grep to be sure

## Checklist
- [x] Branch `feat/maf-1.13-upgrade`
- [x] Version.props: MAF 1.13.0, DevUI previews 260703.1, Roslyn 2.2.29, Qyl 3.1.0/0.2.2,
      servicing line 10.0.8 → 10.0.9 (NU1109 fix: MAF 1.13 floors Bcl.AsyncInterfaces at 10.0.9)
- [x] Reword absolutist comments (Version.props, Directory.Packages.props, AGENTS.md, matrix doc)
- [x] Restore + build green — no source breaks (repo doesn't touch FileAccess/Skills/Foundry surfaces)
- [x] chainOnlyAgentResponses (#6554) surfaced via BuildQylSequential/AsQylSequentialAgent + 3 behavioral tests
- [x] Qyl.OpenTelemetry.AutoInstrumentation 4.0.3 is now on nuget.org → new live sample
      samples/AgentTelemetry.AutoInstrumented (replaces "documented-only" pattern)
- [x] Microsoft.Agents.AI.LocalCodeAct: still unpublished on nuget.org — skipped (external blocker)
- [x] AGENTS.md/READMEs/PackageBoundaryTests/channel-matrix 1.11 → 1.13
- [x] Full test run green (158+ tests)
- [ ] PR opened, CodeRabbit loop, merge when green
