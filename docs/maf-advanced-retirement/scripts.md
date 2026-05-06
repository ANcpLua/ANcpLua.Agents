# Script Retirement Extraction

## Scope

- Source repository: `/Users/ancplua/framework/MAF.Advanced.Patterns`
- Target repository: `/Users/ancplua/framework/ANcpLua.Agents`
- Extracted folder: `scripts/`

## Decision

The BitNet scripts are local developer tooling, not NuGet package surface.
They were extracted into `ANcpLua.Agents/scripts` because this repo owns the
surviving `ANcpLua.Agents.Testing.BitNet` fixture.

## Extracted Scripts

| Script | Target path | Purpose |
|---|---|---|
| `setup-bitnet-local.sh` | `scripts/setup-bitnet-local.sh` | Clone/build pinned BitNet runtime and fetch the GGUF model for local testing. |
| `run-bitnet-local.sh` | `scripts/run-bitnet-local.sh` | Start a local `llama-server` that the fixture can probe through `BITNET_URL`. |
| `test-bitnet.sh` | `scripts/test-bitnet.sh` | Opt-in smoke test for the local BitNet runtime/server stack. |

## Fixture Contract

The current `ANcpLua.Agents.Testing.BitNet.BitNetFixture` is probe-only:

- `BITNET_URL` overrides the default `http://localhost:8080`;
- the fixture probes `/health`;
- the fixture does not auto-launch `llama-server`.

The extracted scripts therefore describe starting an external server and setting
`BITNET_URL`. They do not advertise the larger `MAF.Advanced.Patterns`
auto-launch fixture contract.

## CI Policy

Do not put these scripts into normal CI unless the runner image deliberately
provides the large BitNet runtime/model setup. They are intended for local
developer validation before changing the BitNet fixture or helper scripts.
