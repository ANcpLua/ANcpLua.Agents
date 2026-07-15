# AgentServiceDefaults.Web sample

An ASP.NET Core host that shows the Aspire-style **agent service defaults** from
`ANcpLua.Agents.Hosting.ServiceDefaults` around a Qyl-factory-created MAF agent
that runs fully offline over `ANcpLua.Agents.Testing`'s `FakeChatClient`. A single
`builder.AddQylAgentServiceDefaults()` call registers default health checks;
`app.MapQylAgentEndpoints()` maps the standard `/health` (runs all checks) and
`/alive` (process-up) probes; and a minimal `/run` endpoint drives the agent and
returns its text — no API key, no network.

Telemetry is MAF-native: `QylAgentFactory` is the construction boundary and always
returns the `OpenTelemetryAgent` wrapper, which
emits semantic-convention `invoke_agent` spans (and `execute_tool` spans, since
OpenTelemetry sits below `FunctionInvokingChatClient`) on the
`Experimental.Microsoft.Agents.AI` source. The factory pins `EnableSensitiveData`
to `false` after configuration so the prompt/response bound cannot be widened.
Register that source on a `TracerProvider` to export; this build-only sample
produces spans in-process. The factory receives `app.Services` after
`builder.Build()` so the inner agent and middleware resolve DI services from the host.

```bash
dotnet build samples/AgentServiceDefaults.Web/AgentServiceDefaults.Web.csproj -c Debug
```

This is a build-only showcase; the `app.Run()` call is present for completeness
but the server is not meant to be started as part of the sample suite. If you do
run it, `GET /run` returns the canned reply and emits an `invoke_agent` span,
`GET /health` reports healthy, and `GET /alive` returns 200.
