# AgentDevUI.Governed

This sample shows the Microsoft Agent Framework (MAF) **DevUI** playground and OpenAI-compatible
hosting endpoints layered with **ANcpLua.Agents governance** and **MAF-native OpenTelemetry**, fully
offline. A `WebApplication` registers a single `ticket-agent` built over `FakeChatClient` (seeded with
`.WithFactory(...)`/`.WithResponse(...)`, so no API keys or network are needed). The agent is composed
by `QylAgentFactory`: the factory creates the inner agent with the host DI provider, composes
`UseQylGovernance(...)`, and always returns MAF's `OpenTelemetryAgent` as the outermost wrapper
(conformant gen_ai `invoke_agent` / `execute_tool` spans, sensitive data pinned off). Governance inserts
capability, budget, and concurrency enforcement around every tool invocation. The framework's spans/metrics are collected on
the `Experimental.Microsoft.Agents.AI` source/meter via `AddOpenTelemetry().WithTracing(...).WithMetrics(...)`
and exported over OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (otherwise produced in-process). The
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
