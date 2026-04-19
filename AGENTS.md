# AGENTS.md — ANcpLua.Agents

Instructions for AI coding agents working in this repo.

## What this repo is

MAF-specific runtime helpers + test infrastructure. Split out of `ANcpLua.Roslyn.Utilities` so Roslyn-only consumers (analyzers, generators, foundation libs) don't pull MAF transitively.

## Package layout

```
src/
  ANcpLua.Agents/                  # Runtime helpers (AgentsHelper, WorkflowsHelper, ColorHelper, AIAgent extensions)
  ANcpLua.Agents.Testing/          # FakeChatClient, fixtures, conformance suites
  ANcpLua.Agents.Testing.Workflows/ # WorkflowFixture, WorkflowHarness
tests/
  ANcpLua.Agents.Tests/            # Unit tests
```

All packages target `net10.0`. Foundation Roslyn helpers live in the sibling `ANcpLua.Roslyn.Utilities` repo and are consumed here via plain `<PackageReference>` pinned in `Directory.Packages.props`.

## Conventions

Imported from `ANcpLua.NET.Sdk` (global `AGENTS.md`). Key house rules:

- UTF-8 with BOM on new `.cs` files
- File-scoped namespaces, primary constructors, required init properties
- Switch expressions over if-else chains
- `TimeProvider.System` — never `DateTime.UtcNow`
- No suppression: no `#pragma warning disable`, no `[SuppressMessage]`, no `<NoWarn>` (exception: NU1604/NU1701 TFM-specific in `Directory.Build.props`)
- Async methods use `Async` suffix
- Tests: Arrange / Act / Assert comments, xUnit v3 + MTP

## Key design choices

- **Runtime vs test surface split**: `ANcpLua.Agents` is the runtime package (no test-only types). `ANcpLua.Agents.Testing` adds fakes + fixtures. Keep them separate — consumers should never need both in production.
- **No dogfooded analyzers on the test package**: `ANcpLua.Agents.Testing` relaxes rules that don't fit test-double patterns.
- **MAF version discipline**: bump `Microsoft.Agents.AI.*` in `Version.props` as a group. The whole family ships in lock-step from upstream; mixing versions breaks assembly binding.

## Related repos

- Foundation Roslyn helpers + netstandard2.0 lib: `ANcpLua/ANcpLua.Roslyn.Utilities`
- MSBuild SDK + build conventions: `ANcpLua/ANcpLua.NET.Sdk`
- Analyzers: `ANcpLua/ANcpLua.Analyzers`
