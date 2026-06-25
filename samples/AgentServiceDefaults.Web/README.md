# AgentServiceDefaults.Web sample

An ASP.NET Core host that shows the Aspire-style **agent service defaults** from
`ANcpLua.Agents.Hosting.ServiceDefaults` wrapped around a MAF `ChatClientAgent`
that runs fully offline over `ANcpLua.Agents.Testing`'s `FakeChatClient`. A single
`builder.AddQylAgentServiceDefaults()` call registers default health checks;
`app.MapQylAgentEndpoints()` maps the standard `/health` (runs all checks) and
`/alive` (process-up) probes; and a minimal `/run` endpoint drives the agent and
returns its text — no API key, no network.

Telemetry is MAF-native: the agent is wrapped with `.UseOpenTelemetry()`, which
emits semantic-convention `invoke_agent` spans (and `execute_tool` spans, since
OpenTelemetry sits below `FunctionInvokingChatClient`) on the
`Experimental.Microsoft.Agents.AI` source. `EnableSensitiveData` is left at its
default (`false`), pinned explicitly here so the prompt/response bound is visible.
Register that source on a `TracerProvider` to export; this build-only sample
produces spans in-process. The agent is built from `app.Services` after
`builder.Build()` so the OpenTelemetry middleware can resolve DI services.

```bash
dotnet build samples/AgentServiceDefaults.Web/AgentServiceDefaults.Web.csproj -c Debug
```

This is a build-only showcase; the `app.Run()` call is present for completeness
but the server is not meant to be started as part of the sample suite. If you do
run it, `GET /run` returns the canned reply and emits an `invoke_agent` span,
`GET /health` reports healthy, and `GET /alive` returns 200.
