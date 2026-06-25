# AgentDevUI.Governed

This sample shows the Microsoft Agent Framework (MAF) **DevUI** playground and OpenAI-compatible
hosting endpoints layered with **ANcpLua.Agents governance** and **telemetry**, fully offline. A
`WebApplication` registers a single `ticket-agent` built over `FakeChatClient` (seeded with
`.WithFactory(...)`/`.WithResponse(...)`, so no API keys or network are needed). The agent is
composed through `.AsBuilder().UseAgentRunTelemetry().UseAgentToolTelemetry().UseQylGovernance(...)`:
`AddAgentTelemetry()` wires the qyl-flavored traces/metrics sources, and `UseQylGovernance(...)`
inserts capability, budget, and concurrency enforcement around every tool invocation. The
`lookup_status` tool is gated by an `AgentToolPolicy` requiring the `tickets:read` capability with a
3-attempt / single-concurrent-call budget (`AgentCapabilityContext`, `AgentBudgetEnforcer`,
`AgentConcurrencyLimiter`). OpenAI responses/conversations are mapped via
`MapOpenAIResponses()`/`MapOpenAIConversations()`, and in Development the DevUI playground is mounted
at `/devui` via `AddDevUI()` + `MapDevUI()`.

## Run

```bash
dotnet run --project samples/AgentDevUI.Governed/AgentDevUI.Governed.csproj
```

Then open the DevUI playground at `http://localhost:<port>/devui` and ask the agent about a ticket to
trigger a governed tool call. This is a build-only showcase; the responses are canned by the fake
chat client.
