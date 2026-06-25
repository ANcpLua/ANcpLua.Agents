# AgentApproval.Gate

Showcases human-in-the-loop tool approval for a Microsoft Agent Framework agent using the
ANcpLua.Agents single-call approval gate. A `ChatClientAgent` is built over an offline
`FakeChatClient` (seeded to request an `issue_refund` tool call) and wrapped with
`AIAgentBuilder.UseQylApproval(predicate)` from `ANcpLua.Agents.Governance`. Before every tool
invocation the predicate decides whether the call is approved: when it returns `true` the real
tool runs and the agent answers; when it returns `false` the gate throws
`AgentApprovalDeniedException` (carrying the offending `ToolName`) before the tool can execute.
The program runs the agent twice — once granting approval, once denying it — and prints both
outcomes. No API keys or network access are required.

Combination: MAF `ChatClientAgent` function-invocation middleware x
`ANcpLua.Agents.Governance` (`UseQylApproval` / `AgentApprovalDeniedException`) x
`ANcpLua.Agents.Testing` (`FakeChatClient`).

## Run

```bash
cd /Users/ancplua/ANcpLua.Agents
dotnet run --project samples/AgentApproval.Gate/AgentApproval.Gate.csproj
```
