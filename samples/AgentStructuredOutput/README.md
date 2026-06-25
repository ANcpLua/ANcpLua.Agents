# AgentStructuredOutput

Showcases **typed structured output** from a Microsoft Agent Framework (MAF) agent, fully offline.

It combines MAF's structured-output path (`ChatClientAgent.RunAsync<T>`, which sends a JSON-schema
response format and returns a typed `AgentResponse<T>`) with the ANcpLua.Agents helper
`QylSchemaExtensions.RunQylWithSchemaAsync<T>`. The qyl wrapper pre-attaches a
`JsonStringEnumConverter` to the serializer options, so a record whose fields include an `enum`
just works: the model emits `"condition":"Sunny"` as a string and it deserializes straight into the
`WeatherCondition` enum. That auto-enum behavior is the reason to reach for the qyl extension over
the bare MAF `RunAsync<T>`.

The agent runs over `ANcpLua.Agents.Testing.ChatClients.FakeChatClient`, seeded with one canned JSON
response that matches the `WeatherReport` record. No network calls and no API keys are required.

Because `WeatherReport` is an object-typed schema, MAF does **not** wrap it in a `{"data": ...}`
envelope (only root-level primitives, enums, and arrays get wrapped), so the FakeChatClient returns
the record's JSON directly. The program then prints `response.Result`, the deserialized typed value.

## Run

```bash
cd /Users/ancplua/ANcpLua.Agents
dotnet run --project samples/AgentStructuredOutput/AgentStructuredOutput.csproj
```

Expected output:

```
City:        Vienna
Temperature: 21C
Condition:   Sunny
```
