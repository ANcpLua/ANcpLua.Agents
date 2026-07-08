# CoordinatorTeam.CostSplit

The **coordinator cost-split pattern** on Anthropic's [Managed Agents API](https://platform.claude.com/docs/en/managed-agents/multi-agent): a frontier **coordinator** plans and synthesizes but holds no tools of its own, while cheap **workers** do the token-heavy web reading in their own context-isolated threads and report back distilled findings. Only the workers ever touch a raw web page — that separation is the entire cost story.

This is the **API surface** (`api.anthropic.com` + an API key, per-token console pricing), which is a *different world* from Claude Code's local `/advisor` executor/advisor feature. Do not conflate the two: the `/advisor` model restriction on the Claude Max subscription (which force-substitutes Opus 4.8 for Sonnet) does **not** apply here — this API org has its own model-access policy.

## Model policy (decided)

Default is **all-Fable** — the highest-accuracy configuration (BrowseComp: 90.8% @ ~$40.56/problem, vs Fable-lead + Sonnet-workers at 86.8% @ ~$18.53). The split is the **cost fallback**, not the default.

Nothing is hardcoded or locked — both models are environment variables, so every model stays reachable:

| Variable | Default | Purpose |
|---|---|---|
| `ANTHROPIC_API_KEY` | *(required)* | API-org key — **not** your Claude Max login |
| `TEAM_COORDINATOR_MODEL` | `claude-fable-5` | The planner/synthesizer |
| `TEAM_WORKER_MODEL` | `claude-fable-5` | The readers — set to `claude-sonnet-5` for the cost split |
| `RUN_SOLO_CONTROL` | *(unset)* | `1` also runs a rigor-matched solo-frontier agent and prices both |

## Run

```bash
export ANTHROPIC_API_KEY=sk-ant-...

# Your default — all-Fable, highest accuracy:
dotnet run --project samples/CoordinatorTeam.CostSplit

# The cost-split fallback (Fable lead + Sonnet workers), with the solo control priced too:
TEAM_WORKER_MODEL=claude-sonnet-5 RUN_SOLO_CONTROL=1 \
  dotnet run --project samples/CoordinatorTeam.CostSplit
```

It streams the delegation live (`[spawn]` / `[delegate ->]` / `[report <-]` / `[coordinator]`), prints the synthesized answer, then meters each thread from the API's per-thread cumulative `usage` and prices the run. Prices live in `Program.cs` (`prices`) — the `/v1/models` endpoint reports capabilities but not pricing, so update those two numbers per model when rates change.

## What the API gives you (no SDK required)

This sample is BCL-only (`HttpClient` + `System.Text.Json`) and mirrors the documented REST payloads directly, so it doesn't depend on any SDK exposing the `managed-agents-2026-04-01` beta.

- Coordinator = an agent with `tools: [{ type: agent_toolset_20260401 }]` **and** `multiagent: { type: coordinator, agents: [...] }`. The server auto-provides `create_agent` / `send_to_agent` / `wait_for_agents` / `list_agents`; workers get `submit_result` / `send_to_parent`. You define none of them.
- Workers are ordinary agents with their own `model` and a scoped toolset (here: `web_search` + `web_fetch` only — that scope is also the security boundary, since workers read untrusted web pages).

## Limits worth remembering

- ≤ 20 unique agents in a coordinator's roster; ≤ 25 concurrent threads.
- **One** delegation level — a worker cannot itself coordinate.
- The roster is **snapshotted** at coordinator create/update (pinned versions). Change a worker → recreate/update the coordinator.
- The coordinator **cannot see its workers' prompts** — everything it believes about them must live in its own system prompt.

## When the split does *not* pay

Narrow questions (too little reading to arbitrage), runs where the coordinator answers from memory with no `[spawn]` lines (you paid a frontier round-trip for nothing), and tasks needing frontier judgment on the *raw* material (a cheap reader may summarize away exactly what mattered). Hold any solo-vs-split comparison to **matched verification rigor** or the comparison is meaningless — that's what `RUN_SOLO_CONTROL` enforces.
