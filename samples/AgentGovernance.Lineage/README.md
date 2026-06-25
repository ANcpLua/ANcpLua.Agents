# AgentGovernance.Lineage

Showcases the ANcpLua.Agents bounded-autonomy primitives that keep a parent agent from spawning
child agents without limit. A parent agent spawns children over an offline `FakeChatClient`, and
three independent governance mechanisms each enforce a different limit and print a lineage summary:

- **`AgentCallLineage`** (`TryEnter` / `Current` / `Complete` / `FormatLineageSummary`) — threads
  an `AsyncLocal` lineage through the call chain and enforces a per-root *spawn budget*. The root
  is entered once; depth-1 children are spawned in a loop. With `maxSpawns: 3` the 4th spawn is
  refused and the refusal reason names the lineage.
- **`AgentSpawnTracker`** (`Register` / `GetDescendantCount` + `AgentSpawnLimitExceededException`) —
  tracks linear parent→child nesting and enforces a *depth limit* taken from `AgentToolPolicy`
  (`MaxAttempts` is the depth cap). A grandchild past the cap throws the typed exception.
- **`AgentCallGuard`** (`FromEnvironment` / `Wrap` / `RecordCall`) — caps tool invocations per call.
  `Wrap` makes the agent's own tool loop self-record; `RecordCall` against a tiny `FromEnvironment`
  cap trips an `OperationCanceledException` carrying a partial-results summary.

Combination: MAF `ChatClientAgent` x `ANcpLua.Agents.Governance` x
`ANcpLua.Agents.Testing.ChatClients.FakeChatClient`. No API keys, no network.

## Run

```bash
dotnet run --project samples/AgentGovernance.Lineage/AgentGovernance.Lineage.csproj
```
