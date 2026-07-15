# AgentTools.Governed

Showcases governed agent tools, fully offline (no API keys), combining **Microsoft Agent Framework**
(the chat-client-agent function-invoking loop and `AIAgentBuilder.Use` middleware) with
**ANcpLua.Agents.Governance** and **ANcpLua.Agents.Testing**'s `FakeChatClient`. Tools are wrapped
with an `AgentToolPolicy` (max attempts + required capabilities) enforced by an
`AgentBudgetEnforcer`, an `AgentConcurrencyLimiter`, and an `AgentCapabilityContext`. Two
enforcement paths are demonstrated side by side, and in both cases the agent keeps running after a
tool is blocked because MAF reports each governance failure back to the model as a
`FunctionResultContent` carrying the thrown governance exception: **Part 1** uses
`QylToolSet.From<T>(...)` to project a host type's methods into per-tool `GovernedAIFunction`
wrappers, where a required-but-ungranted capability (`billing:write`) denies a refund tool before
its body runs; **Part 2** uses `AIAgentBuilder.UseQylGovernance(...)` with a `policyResolver` so
a `MaxAttempts: 1` budget trips on the second call to an invoice-lookup tool. The program prints,
for each part, the agent's final reply, the governance exception types that were enforced, and how
many times each tool body actually executed — proving the policy was honored. Both agents are created
through `QylAgentFactory`, which keeps the governance pipeline inside mandatory MAF telemetry.

## Run

```bash
dotnet run --project samples/AgentTools.Governed/AgentTools.Governed.csproj -c Debug
```

Expected output (abridged):

```
== PART 1: QylToolSet.From<T> (per-tool GovernedAIFunction) ==
  enforced    : AgentCapabilityDeniedException
  body ran    : 0 refund(s) (expected 0 — capability denied)

== PART 2: AIAgentBuilder.UseQylGovernance (run-level middleware) ==
  enforced    : AgentBudgetExceededException, ...
  body ran    : 1 lookup(s) (expected 1 — MaxAttempts=1)
```
