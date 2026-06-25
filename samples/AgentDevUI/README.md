# AgentDevUI sample

Launches the **Microsoft Agent Framework DevUI** playground against a key-free
fake agent, with MAF-native OpenTelemetry (`invoke_agent` / `execute_tool`) in
front of it.

```bash
dotnet run --project samples/AgentDevUI
```

Then open <http://localhost:5280/devui>. Pick **ticket-agent** and send a
message — the first message scripts a `lookup_status` tool call (visible in the
DevUI trace); later messages get a canned reply, so the agent stays responsive
with no live model and no API key.

## What it wires

- `AddAIAgent(name, factory)` registers a `FakeChatClient`-backed `ChatClientAgent`
  so DevUI can discover and drive it — `Microsoft.Agents.AI.Hosting`.
- `AddOpenAIResponses()` / `AddOpenAIConversations()` + their `Map*` calls — the
  OpenAI Responses/Conversations surface DevUI talks to. **Required**: `MapDevUI`
  alone serves no agents without these — `Microsoft.Agents.AI.Hosting.OpenAI`.
- `AddDevUI()` / `MapDevUI()` — the playground at `/devui`, mapped only in the
  Development environment — `Microsoft.Agents.AI.DevUI`.
- `UseAgentTelemetry()` — wraps the discovered agent in MAF-native
  `OpenTelemetryAgent` (semconv `invoke_agent` / `execute_tool` spans, sensitive
  data off) — `ANcpLua.Agents.Instrumentation`.

## Why preview packages

DevUI, `Hosting`, and `Hosting.OpenAI` ship **preview/alpha only** — there is no
stable release. The pins in `Version.props` are the `1.11.0-*` builds, which
depend on the **stable** `Microsoft.Agents.AI 1.11.0` core the rest of the repo
already uses, so nothing else moves off the stable line. These packages are
referenced **only** by this sample (`IsPackable=false`); no shipped library
takes a DevUI dependency.

## Security

DevUI is for local development only. Its endpoints expose agent instructions,
tool definitions, and model ids, so it **rejects non-loopback callers** by
default and this sample maps it only when `IsDevelopment()`. To front it with
real auth, see `DevUIOptions` (`AllowRemoteAccess`, `AuthToken`,
`ConfigureEndpoints`). Do not expose it to untrusted callers.
