# MAF.Advanced.Patterns Retirement Plan

## Objective

Make `/Users/ancplua/framework/MAF.Advanced.Patterns` disposable as a standalone
repo after coherent, channel-safe ideas have landed in
`/Users/ancplua/framework/ANcpLua.Agents`.

`ANcpLua.Agents` is the target and authoritative successor.
`MAF.Advanced.Patterns` is the retiring source repository and archive candidate.

## Gates Before Archive Or Delete

1. `ANcpLua.Agents` restore, build, test, and local pack pass.
2. Package-boundary tests prove stable packages do not take preview/RC/alpha
   dependencies.
3. The retirement PR is green in CI.
4. NuGet trusted publishing readiness is confirmed if the PR reaches `main`.
5. No downstream consumer still depends on unpublished or deleted
   `MAF.Advanced.Patterns` surfaces.
6. Deferred surfaces are documented as dropped, deferred, or future package work.
7. Owner signs off that no rollback path requires the old repo.

## Current Retirement Checklist

- Keep `ANcpLua.Agents` package IDs and namespaces aligned.
- Preserve stable package isolation from preview, RC, and alpha MAF dependencies.
- Keep `Qyl*` public method prefixes on migrated facades.
- Retain only facades that guard and delegate, plus small workflow/testing
  helpers with focused tests.
- Do not migrate qyl-specific config, provider env vars, or telemetry naming as
  normative ANcpLua behavior.
- Keep A2A, AG-UI, MCP, generic declarative, standalone Durable Task Scheduler,
  Purview, Cosmos NoSQL, Copilot Studio, and GitHub Copilot out of the current
  stable package spine unless a future isolated package is explicitly designed.
- Keep local BitNet scripts as opt-in developer tooling only; they are not
  publish or runtime package surface.
- Leave deletion/archive of the old repo until CI and publish gates are proven.

## Extracted Local Tooling

The local BitNet helper scripts have been extracted to `scripts/`:

- `scripts/setup-bitnet-local.sh`
- `scripts/run-bitnet-local.sh`
- `scripts/test-bitnet.sh`

They match the current probe-only `ANcpLua.Agents.Testing.BitNetFixture`
contract: start a local `llama-server` externally and set `BITNET_URL`.

## Archive Checklist

- [ ] `ANcpLua.Agents` restore, build, test, and local pack pass.
- [ ] Package-boundary tests pass.
- [ ] Deferred protocol/provider surfaces remain documented as dropped/deferred.
- [ ] Local-only scripts are retained here or explicitly dropped.
- [ ] No active project depends on unpublished or deleted `MAF.Advanced.Patterns`
      outputs.
- [ ] CI/publish gates are green.
- [ ] Owner signs off that no rollback path requires the old repo.
