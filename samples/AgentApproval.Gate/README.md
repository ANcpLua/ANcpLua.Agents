# AgentApproval.Gate

Two ways to gate a side-effecting Microsoft Agent Framework tool, side by side, and when to use each.

## Path A — deterministic gate (`UseQylApproval`)

`ANcpLua.Agents.Governance.UseQylApproval(predicate)` runs a predicate before every tool call.
Denial throws `AgentApprovalDeniedException` straight to the caller and the tool never runs.
Single-call, no extra round trip, no conversation state. Reach for it when approval is a
synchronous in-process decision (feature flag, RBAC claim, kill switch) and a denied call should
fail the run.

The gate is function-invocation middleware, so it runs inside the `FunctionInvokingChatClient`
(FICC) loop. FICC normally captures a tool exception and feeds it back to the model
(`MaximumConsecutiveErrorsPerRequest = 3` by default), which would swallow a denial. The sample
pre-builds the FICC with `MaximumConsecutiveErrorsPerRequest = 0` so it rethrows immediately —
`ChatClientAgent` reuses a FICC already present on the chat client rather than inserting its own.

## Path B — native human-in-the-loop (`ApprovalRequiredAIFunction`)

Wrapping a tool in MEAI's `ApprovalRequiredAIFunction` makes the agent **pause** on the tool call:
the run returns a `ToolApprovalRequestContent` instead of a result. A human decides out of band, then
the caller resumes the same `AgentSession` by sending each request's
`CreateResponse(approved, reason)` back as a user message — the MAF `ToolApprovalRequestContent`
protocol. Multi-turn. Reach for it when approval needs a person, an external system, or time the
request can't block on.

The sample uses `new ApprovalRequiredAIFunction(tool)` directly — that is the whole of what the
`QylApprovalGate.RequireQylApproval` wrapper does.

Both paths run fully offline against a seeded `ANcpLua.Agents.Testing.FakeChatClient`. No API keys or
network access are required. Both agents are constructed by `QylAgentFactory`; the deterministic
approval middleware remains inside the factory's mandatory MAF-native telemetry wrapper.

## Run

```bash
cd /path/to/ANcpLua.Agents   # repo root
dotnet run --project samples/AgentApproval.Gate/AgentApproval.Gate.csproj
```
