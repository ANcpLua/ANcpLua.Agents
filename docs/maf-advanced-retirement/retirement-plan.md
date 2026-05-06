# MAF.Advanced.Patterns Retirement Plan

## Objective

Make `/Users/ancplua/framework/MAF.Advanced.Patterns` disposable as a standalone repo after coherent, channel-safe ideas have landed in `/Users/ancplua/framework/ANcpLua.Agents`.

## Gates Before Archive Or Delete

1. `ANcpLua.Agents` restore, build, test, and local pack pass.
2. The retirement PR is green in CI.
3. NuGet trusted publishing readiness is confirmed if the PR reaches `main`.
4. No downstream consumer still depends on unpublished or deleted `MAF.Advanced.Patterns` surfaces.
5. `MAF.Advanced.Patterns` remaining value is either migrated, explicitly deferred, or documented as dropped.

## Current Retirement Checklist

- Keep `ANcpLua.Agents` package IDs and namespaces aligned.
- Preserve stable package isolation from preview, RC, and alpha MAF dependencies.
- Keep `Qyl*` public method prefixes on migrated facades.
- Retain only facades that guard and delegate, plus small workflow/testing helpers with focused tests.
- Do not migrate qyl-specific config, provider env vars, or telemetry naming as normative ANcpLua behavior.
- Leave deletion/archive of the old repo until CI and publish gates are proven.
