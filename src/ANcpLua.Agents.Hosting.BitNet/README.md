# ANcpLua.Agents.Hosting.BitNet

Consumer toolkit for Microsoft Agent Framework — local-LLM hosting via [bitnet.cpp](https://github.com/microsoft/BitNet).

Alpha-channel BitNet hosting facades and client factory for Microsoft Agent Framework consumers. Targets the OpenAI-compatible `/v1` surface that `bitnet.cpp`'s `llama-server` exposes.

- Compatible with: Microsoft.Agents.AI 1.4.x, Microsoft.Extensions.AI 10.5.x
- Tested against: bitnet.cpp built from `microsoft/BitNet` (b1.58-2B-4T weights, `ggml-model-i2_s.gguf`)
- Channel: alpha. Keep this package isolated from stable and preview consumers unless explicitly intended.

## Why a dedicated hosting package

Stock `Microsoft.Extensions.AI.OpenAI` works against any OpenAI-compatible endpoint — but llama-server builds older than [ggml-org/llama.cpp#19831](https://github.com/ggml-org/llama.cpp/pull/19831) (merged 2026-02-23) silently ignore the SDK-emitted `max_completion_tokens` field and generate to the context limit. This package promotes the `LegacyMaxTokensPolicy` shim out of test-only territory and ships it as part of the runtime path.

## Usage — three modes

### Mode 1 — Aspire-style one-liner

```csharp
builder.AddQylBitNetChatClient("bitnet");
// reads ConnectionStrings:bitnet = "http://localhost:8080"
```

### Mode 2 — programmatic configuration (keyed multi-endpoint)

```csharp
builder.AddQylBitNetChatClient("local", o =>
{
    o.Endpoint = new Uri("http://localhost:8080");
    o.Model    = "bitnet-b1.58-2B-4T";
    o.ApiPath  = "/v1";
});
```

### Mode 3 — assembly attribute + bundled source generator

```csharp
[assembly: QylBitNetEndpoint("bitnet", "http://localhost:8080", Model = "bitnet-b1.58-2B-4T")]

// Program.cs — one call wires every declared endpoint
builder.AddDiscoveredQylBitNetClients();
```

Resolve via keyed services:

```csharp
public sealed class MyAgent([FromKeyedServices("bitnet")] IChatClient chat) { ... }
```

## What gets registered

- `IChatClient` keyed by the connection name (singleton)
- `IOptions<QylBitNetClientOptions>` bound to `BitNet:<name>:*` config section
- Health check named `bitnet:<name>` against `<endpoint>/health`
- OpenTelemetry decoration via `Microsoft.Extensions.AI.UseOpenTelemetry()` — emits standard `gen_ai.*` spans/meters

## Environment overrides

For test scenarios and the [BitNetFixture](../ANcpLua.Agents.Testing/BitNet/BitNetFixture.cs) contract:

- `BITNET_URL` — overrides `Endpoint`
- `BITNET_API_PATH` — overrides `ApiPath` (default `/v1`)
- `BITNET_MODEL` — overrides `Model` (default `bitnet-b1.58-2B-4T`)

Env vars win over config so existing fixture consumers keep working.
