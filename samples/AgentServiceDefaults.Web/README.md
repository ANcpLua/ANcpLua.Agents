# AgentServiceDefaults.Web sample

An ASP.NET Core host that shows the Aspire-style **agent service defaults** from
`ANcpLua.Agents.Hosting.ServiceDefaults` wrapped around a MAF `ChatClientAgent`
that runs fully offline over `ANcpLua.Agents.Testing`'s `FakeChatClient`. A single
`builder.AddQylAgentServiceDefaults()` call registers MAF/ANcpLua agent telemetry
plus default health checks; `app.MapQylAgentEndpoints()` maps the standard
`/health` (runs all checks) and `/alive` (process-up) probes; and a minimal
`/run` endpoint drives the agent and returns its text — no API key, no network.

The agent is built from `app.Services` after `builder.Build()` because the
`UseAgentRunTelemetry()` middleware resolves its instrumentation from DI.

```bash
dotnet build samples/AgentServiceDefaults.Web/AgentServiceDefaults.Web.csproj -c Debug
```

This is a build-only showcase; the `app.Run()` call is present for completeness
but the server is not meant to be started as part of the sample suite. If you do
run it, `GET /run` returns the canned reply, `GET /health` reports healthy, and
`GET /alive` returns 200.
