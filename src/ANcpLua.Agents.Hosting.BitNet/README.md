# ANcpLua.Agents.Hosting.BitNet

Consumer toolkit for Microsoft Agent Framework — local-LLM hosting via Microsoft's [BitNet b1.58](https://github.com/microsoft/BitNet) over the OpenAI-compatible `/v1` surface.

Alpha-channel package. Keep isolated from stable/preview consumers unless explicitly intended.

- Compatible with: Microsoft.Agents.AI 1.4.x
- Tested against: Microsoft.Agents.AI 1.4.0 + Microsoft.Extensions.AI 10.5.x
- Capability tested against: BitNet b1.58 2B-4T weights served by Microsoft's prebuilt `bitnet.cpp` Docker image

## Standing up a BitNet server

The hosting package only speaks HTTP to an OpenAI-compatible endpoint — it never builds, downloads, or spawns the binary. You can satisfy that contract any way you like; Microsoft's prebuilt Docker image is the easiest path:

```sh
scripts/bitnet-docker.sh start    # idempotent — stops any prior container first
export BITNET_URL=http://localhost:11434
```

The script pins the image by digest (`sha256:9d5f7f4e...cd243a` as of 2026-05-12), so byte-identical runs are guaranteed until you intentionally re-resolve.

What it bundles: `bitnet.cpp`, the `b1.58-2B-4T` GGUF weights, the patched `llama-server`, all under `/v1/chat/completions` on port 11434. No Python, cmake, LUT codegen, or `git clone` involved.

If your environment cannot pull this image (air-gapped hosts, vendor mirrors, custom builds), point `BITNET_URL` at any other OpenAI-compatible `/v1/chat/completions` endpoint — LM Studio, vLLM with a BitNet build, your own `llama-server` build, a private inference gateway. The hosting package does not care how the server got there. See *Other OpenAI-compatible servers* below.

Health check:

```sh
curl -fsS http://localhost:11434/health && echo ok
```

Stop:

```sh
scripts/bitnet-docker.sh stop
```

### Pinning by digest (production / supply-chain)

The `scripts/bitnet-docker.sh start` command already runs digest-pinned. If you want to re-resolve to a newer Microsoft build, the recipe is:

```sh
docker buildx imagetools inspect \
  mcr.microsoft.com/appsvc/docs/sidecars/sample-experiment:bitnet-b1.58-2b-4t-gguf \
  --format '{{json .Manifest.Digest}}'
```

Edit the `IMAGE=` constant at the top of `scripts/bitnet-docker.sh` with the new digest, commit, done.

### Other OpenAI-compatible servers

LM Studio, vLLM with a BitNet build, a private inference gateway, your own fork — all work. Point `BITNET_URL` at them.

**Note on Ollama**: as of mid-2026 Ollama does not run BitNet b1.58 (upstream `llama.cpp` lacks the `i2_s` tensor type that BitNet requires; Ollama inherits that gap — see ollama/ollama#10334).

## Why a dedicated hosting package

Stock `Microsoft.Extensions.AI.OpenAI` works against any OpenAI-compatible endpoint — but `llama-server` builds older than [ggml-org/llama.cpp#19831](https://github.com/ggml-org/llama.cpp/pull/19831) (merged 2026-02-23) silently ignore the SDK-emitted `max_completion_tokens` field and generate to the context limit. This package promotes the `LegacyMaxTokensPolicy` shim out of test-only territory and ships it as part of the runtime path. Once Microsoft's BitNet fork picks up the upstream merge, the policy becomes a no-op self-deleting decorator.

## Zero-ceremony path — `ANcpLua.NET.Sdk.BitNet`

If your repo already uses an MSBuild SDK from [ANcpLua/ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk), switch the variant from `.Web` to `.BitNet` and you get this hosting package as an implicit `PackageReference` — no `<PackageReference Include="ANcpLua.Agents.Hosting.BitNet" />` line of your own needed. The pinned version lives in the SDK's `Version.props` and ships in lockstep with releases here.

```json
// global.json
{ "msbuild-sdks": { "ANcpLua.NET.Sdk.BitNet": "3.4.31" } }
```

```xml
<!-- consumer .csproj -->
<Project Sdk="ANcpLua.NET.Sdk.BitNet">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <NoWarn>$(NoWarn);ANCPLBITNET001</NoWarn>
  </PropertyGroup>
</Project>
```

**Hard requirement — Central Package Management.** The SDK forces `ManagePackageVersionsCentrally=true` and the enforcement target errors if the consumer overrides it. Ship a `Directory.Packages.props` at or above the consumer (an empty one with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` is enough); without it, restore fails with `NU1015` on the SDK-injected analyzers.

Then call `builder.AddQylBitNetChatClient()` in `Program.cs` exactly as below.

## Usage — four modes

### Mode 0 — zero config (defaults to `BITNET_URL`)

```csharp
builder.AddQylBitNetChatClient();   // connection name defaults to "bitnet"
```

Resolves:

```csharp
public sealed class MyAgent([FromKeyedServices("bitnet")] IChatClient chat) { ... }
```

### Mode 1 — Aspire-style named connection

```csharp
builder.AddQylBitNetChatClient("bitnet");
// reads ConnectionStrings:bitnet = "http://localhost:11434"
```

### Mode 2 — programmatic configuration (keyed multi-endpoint)

```csharp
builder.AddQylBitNetChatClient("local", o =>
{
    o.Endpoint = new Uri("http://localhost:11434");
    o.Model    = "bitnet-b1.58-2B-4T";
    o.ApiPath  = "/v1";
});
```

### Mode 3 — assembly attribute + bundled source generator

```csharp
[assembly: QylBitNetEndpoint("bitnet", "http://localhost:11434", Model = "bitnet-b1.58-2B-4T")]

// Program.cs — one call wires every declared endpoint
builder.AddDiscoveredQylBitNetClients();
```

## What gets registered

- `IChatClient` keyed by the connection name (singleton)
- `IOptions<QylBitNetClientOptions>` bound to `BitNet:<name>:*` config section
- Health check named `bitnet:<name>` against `<endpoint>/health`
- OpenTelemetry decoration via `Microsoft.Extensions.AI.UseOpenTelemetry()` — emits standard `gen_ai.*` spans/meters

## Environment overrides

For test scenarios and the `BitNetFixture` contract (`ANcpLua.Agents.Testing.BitNet.BitNetFixture`):

- `BITNET_URL` — overrides `Endpoint`. When unset, the fixture auto-starts the Docker image itself (unless `BITNET_FIXTURE_NO_DOCKER` is truthy).
- `BITNET_API_PATH` — overrides `ApiPath` (default `/v1`)
- `BITNET_MODEL` — overrides `Model` (default `bitnet-b1.58-2B-4T`)
- `BITNET_FIXTURE_NO_DOCKER` — set truthy to opt the fixture out of auto-Docker

Env vars win over config so existing fixture consumers keep working.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `BitNet endpoint is not configured` at startup | `BITNET_URL` not set and no `ConnectionStrings:<name>` / `BitNet:<name>:Endpoint` bound | Run `scripts/bitnet-docker.sh start` then `export BITNET_URL=http://localhost:11434`, or pass `configure: o => o.Endpoint = ...` |
| Health check `bitnet:<name>` is Unhealthy | server not listening, or wrong port | `curl $BITNET_URL/health` to confirm; check container with `scripts/bitnet-docker.sh status` |
| Model generates until context fills, ignores token cap | running against a `llama-server` older than llama.cpp PR #19831 without our shim | confirm `Microsoft.Extensions.AI.OpenAI` is going through `QylBitNetChatClientFactory.Create` — the `LegacyMaxTokensPolicy` is applied there automatically |
| `Unexpected end of input` / GGUF errors on stock Ollama | Ollama uses upstream llama.cpp which lacks `i2_s` | Ollama is not a target — use the Docker image |
