# ANcpLua.Agents.Hosting.BitNet

Consumer toolkit for Microsoft Agent Framework — local-LLM hosting via Microsoft's [BitNet b1.58](https://github.com/microsoft/BitNet) over the OpenAI-compatible `/v1` surface.

Alpha-channel package. Keep isolated from stable/preview consumers unless explicitly intended.

- Compatible with: Microsoft.Agents.AI 1.4.x
- Tested against: Microsoft.Agents.AI 1.4.0 + Microsoft.Extensions.AI 10.5.x
- Capability tested against: BitNet b1.58 2B-4T weights served by `bitnet.cpp`'s `llama-server`

## Standing up a BitNet server (pick one)

The hosting package only speaks HTTP to an OpenAI-compatible endpoint — it never builds, downloads, or spawns the binary. You can satisfy that contract any way you like. From easiest to most reproducible:

### Option A — Microsoft's prebuilt Docker image (recommended)

```sh
docker run -d --rm -p 11434:11434 \
  --name bitnet \
  mcr.microsoft.com/appsvc/docs/sidecars/sample-experiment:bitnet-b1.58-2b-4t-gguf

export BITNET_URL=http://localhost:11434
```

That's it. The image bundles `bitnet.cpp`, the `b1.58-2B-4T` GGUF weights, and the patched `llama-server`; it exposes `/v1/chat/completions` on port 11434 directly. No Python, no cmake, no LUT codegen, no `git clone`. First `docker pull` is ~2 GB and ~2 minutes; restarts are instant.

Health check:

```sh
curl -fsS http://localhost:11434/health && echo ok
```

#### Pinning by digest (production / supply-chain)

The tag `bitnet-b1.58-2b-4t-gguf` is mutable — Microsoft can repoint it to a rebuilt image without warning. For reproducible deployments (production, anything with a security review, anything you'll point a CI fixture at), pin to the immutable image digest:

```sh
# 1. Resolve the current digest for the tag (no full pull required)
docker buildx imagetools inspect \
  mcr.microsoft.com/appsvc/docs/sidecars/sample-experiment:bitnet-b1.58-2b-4t-gguf \
  --format '{{json .Manifest.Digest}}'
# → "sha256:abcd1234..."

# 2. Run pinned to that digest (the tag is no longer consulted)
docker run -d --rm -p 11434:11434 --name bitnet \
  mcr.microsoft.com/appsvc/docs/sidecars/sample-experiment@sha256:abcd1234...
```

Same image bytes every time, regardless of upstream tag movement. Re-resolve the digest only when you intentionally want to take a newer build.

### Option B — build from source via the repo script

For air-gapped builds, custom kernels, or pinning to a specific `microsoft/BitNet` commit:

```sh
./scripts/setup-bitnet-local.sh    # ~10–15 min first run; idempotent
./scripts/run-bitnet-local.sh &     # foreground llama-server on :8080
export BITNET_URL=http://localhost:8080
```

This is what `BitNetFixture` runs in CI. Requires Python 3, cmake, git, clang. The script clones `microsoft/BitNet` at a pinned commit, code-generates the LUT kernels for the current arch (`codegen_tl1.py` on ARM64, `codegen_tl2.py` on x86), patches `server.cpp` for `max_completion_tokens`, builds `llama-server`, and fetches the GGUF model with SHA256 verification. Reproducible byte-for-byte; slow first run.

### Option C — anything else that speaks OpenAI `/v1`

LM Studio, vLLM with a BitNet build, a private inference gateway, a fork of `bitnet.cpp` you maintain yourself — all work. Point `BITNET_URL` at it.

**Note on Ollama**: as of mid-2026 Ollama does not run BitNet b1.58 (upstream `llama.cpp` lacks the `i2_s` tensor type that BitNet requires; Ollama inherits that gap — see ollama/ollama#10334). Use Options A or B for BitNet specifically.

## Why a dedicated hosting package

Stock `Microsoft.Extensions.AI.OpenAI` works against any OpenAI-compatible endpoint — but `llama-server` builds older than [ggml-org/llama.cpp#19831](https://github.com/ggml-org/llama.cpp/pull/19831) (merged 2026-02-23) silently ignore the SDK-emitted `max_completion_tokens` field and generate to the context limit. This package promotes the `LegacyMaxTokensPolicy` shim out of test-only territory and ships it as part of the runtime path. Once Microsoft's BitNet fork picks up the upstream merge, the policy becomes a no-op self-deleting decorator.

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

- `BITNET_URL` — overrides `Endpoint` (e.g. `http://localhost:11434` for Option A, `http://localhost:8080` for Option B)
- `BITNET_API_PATH` — overrides `ApiPath` (default `/v1`)
- `BITNET_MODEL` — overrides `Model` (default `bitnet-b1.58-2B-4T`)

Env vars win over config so existing fixture consumers keep working.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `BitNet endpoint is not configured` at startup | `BITNET_URL` not set and no `ConnectionStrings:<name>` / `BitNet:<name>:Endpoint` bound | Set `BITNET_URL` per Option A or B, or pass `configure: o => o.Endpoint = ...` |
| Health check `bitnet:<name>` is Unhealthy | server not listening, or wrong port | `curl $BITNET_URL/health` to confirm; check container is running (`docker ps`) |
| Model generates until context fills, ignores token cap | running against a `llama-server` older than llama.cpp PR #19831 without our shim | confirm `Microsoft.Extensions.AI.OpenAI` is going through `QylBitNetChatClientFactory.Create` — the `LegacyMaxTokensPolicy` is applied there automatically |
| `Unexpected end of input` / GGUF errors on stock Ollama | Ollama uses upstream llama.cpp which lacks `i2_s` | Use Option A or B; Ollama is not a target |
