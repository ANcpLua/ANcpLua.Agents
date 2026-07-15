# AgentApiContracts

Showcases a Microsoft Agent Framework (MAF) agent producing a **typed qyl public-API DTO**,
completely offline.

**Combination:** MAF `AIAgent.RunAsync<T>` (structured output)
× ANcpLua.Agents `RunQylWithSchemaAsync<T>` facade
× ANcpLua.Agents.Instrumentation `QylAgentFactory`
× `Qyl.Api.Contracts.ClearTelemetryResponse`.

The agent is built with the ANcpLua `QylAgentFactory` over an offline
`FakeChatClient` (namespace `ANcpLua.Agents.Testing.ChatClients`) seeded with a single JSON
payload — no network and no API keys. `RunQylWithSchemaAsync<ClearTelemetryResponse>` runs
MAF's structured-output path with an enum-friendly, Web-cased `JsonSerializerOptions`, so the
returned `AgentResponse<ClearTelemetryResponse>.Result` is a populated, strongly-typed
`Qyl.Api.Contracts` DTO rather than a raw string. The program then prints each field.

`ClearTelemetryResponse` is a plain object DTO with all-primitive `required` members, so the
seeded reply is just the bare JSON object (no schema wrapper) and the camelCase keys map to the
DTO's `[JsonPropertyName]` declarations.

## Run

```bash
cd /path/to/ANcpLua.Agents   # repo root
dotnet run --project samples/AgentApiContracts/AgentApiContracts.csproj
```

Expected output (values come from the seeded JSON):

```
Typed qyl DTO: Qyl.Api.Contracts.ClearTelemetryResponse
  type             = clear_telemetry_result
  spansDeleted     = 128
  logsDeleted      = 412
  profilesDeleted  = 7
  sessionsDeleted  = 3
  consoleCleared   = 1
```
