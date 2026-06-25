# AgentConditionalTools

Showcases **conditional tool exposure**: an agent whose tools appear only on the conversation turns
that actually need them. The combination is MAF `ChatClientAgent` + `AIContextProvider` x
ANcpLua.Agents `QylContextExtensions.WithQylConditionalTools` / `QylConditionalToolProvider.Register`
x `ANcpLua.Agents.Testing.FakeChatClient` — fully offline, no API keys.

A `QylConditionalToolProvider` is registered on `ChatClientAgentOptions` with one rule: a billing tool
pack (`refund_order`, `lookup_invoice`) gated behind a predicate that matches messages mentioning
"refund", "invoice", or "billing". On each invocation the provider inspects the inbound messages and
appends its tools to the `AIContext` only when the predicate matches; MAF then materializes those tools
into the `ChatOptions` it sends to the chat client. The sample runs two prompts on fresh sessions — one
billing prompt (tools active) and one general-knowledge prompt (no tools) — and reads the tools back
from `FakeChatClient.LastOptions.Tools` to print exactly which tools were active per turn.

## Run

```bash
dotnet run --project samples/AgentConditionalTools/AgentConditionalTools.csproj
```

Expected output: the billing prompt reports `Active tools : refund_order, lookup_invoice`
while the general prompt reports `Active tools : (none)`.
