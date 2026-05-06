# Copilot review instructions

Microsoft Agent Framework (MAF) consumer toolkit. Ships 10 NuGet packages:
stable spine (`ANcpLua.Agents`, `.Workflows`, `.Testing`, `.Testing.Workflows`),
preview hosting (`.Hosting.Azure`, `.Hosting.Foundry`, `.Hosting.Anthropic`,
`.Hosting.DevUI`), RC1 Foundry (`.Foundry`), and alpha OpenAI hosting
(`.Hosting.OpenAI`). All target `net10.0`. Pinned to `Microsoft.Agents.AI`
1.4.0 stable with package-specific preview, rc1, and alpha MAF dependencies in
`Directory.Packages.props`. `Microsoft.Extensions.AI` is 10.5.0. CPM enforced.

## Reviewer focus

- Review **changes in this PR**, not the whole repo. The diff is the assignment.
- Skip files whose first ~3 lines contain `// Ported from <upstream>`,
  `// Generated`, or `// Auto-generated`. Their contract is the upstream's,
  not ours; surface a one-line note instead of line-level findings.
- Skip files in `node_modules/`, `dist/`, `build/`, `bin`, `obj`, generated
  Roslyn artifacts (`*.g.cs`), and lockfiles (`package-lock.json`,
  `pnpm-lock.yaml`, `yarn.lock`).
- Skip vendored fixtures under `**/fixtures/**` and `**/test-data/**` —
  they're deliberately broken or deliberately verbatim.

## Coordinate with other reviewers

- CodeRabbit and Claude Code Review run on the same PR. **Don't repeat
  findings they've already raised** — read the existing review comments
  before posting.
- If CodeRabbit has labeled a finding `false_positive` or the human author
  has marked it resolved, don't re-raise it.

## Package rules

- Package IDs and namespaces must match. `Qyl*` method prefixes stay; `Qyl.*`
  namespaces do not.
- Stable packages must not reference MAF preview, RC, or alpha packages. The
  packaging tests enforce this channel boundary.
- MAF version pin lives in `Directory.Packages.props`. Don't bump
  `Microsoft.Agents.AI` or `Microsoft.Extensions.AI` in a feature PR — version
  bumps are their own commits with downstream impact analysis.
- `LangVersion=preview` is on. C# 14 features (file-scoped namespaces, primary
  constructors, required init, `params ReadOnlySpan<T>`) are repo defaults.
- `ANcpLua.Agents.Testing.Workflows/` includes harvested copies of MAF 1.4.0
  internals (under `Internals/`). Don't refactor those toward "cleaner" shape —
  they exist verbatim for upstream parity, and `RS0030` (banned
  `ArgumentNullException.ThrowIfNull`) is suppressed there because upstream uses
  the banned API.

## Style

- Group findings by file, not by severity, when there are >5.
- Don't suggest renames of public exports without a clear caller-side
  benefit. The cost of a rename is paid by every consumer.
- Don't suggest adding tests "for completeness" — only when the changed
  contract is uncovered by existing tests.

## Allowed suppressions

- Allow-listed suppressions in `ANcpLua.Agents.Testing.csproj`: `CA1002`,
  `CA1034`, `CA1305`, `CA1707`, `CA1816`, `CA1819`, `CA1826`, `CA2000`, `CS1591`,
  `AL0014`, `AL0025`, `AL0131`, `IDE1006`, `IDE0060`, `IDE0059` — the file's own
  comment documents these as MAF-upstream-parity concessions.
- `MEAI001` suppression in `Testing.Workflows` when it mirrors MAF workflow
  experimental API diagnostics.
- `OPENAI001` / `MEAI001` suppressions in Foundry and hosting boundary projects
  when they mirror upstream MAF experimental API diagnostics.
- `NU1604` in `Directory.Build.props` (CPM transitive-pin warning).
- `RS0030` inside `Internals/` directories (harvested upstream code).
- Missing XML doc comments on `internal` types or harvested upstream helpers.

## Project conventions to respect

- Node code: ESM, Node >=20, no external runtime deps unless already declared
  in `package.json`.
- .NET code: nullable enabled, central package management
  (`Directory.Packages.props`), `Version.props` is the single owner of
  versions — never edit `<Version>` lines directly.
- Don't suggest patterns that contradict `CLAUDE.md`, `AGENTS.md`, or the
  repo's `.coderabbit.yaml` `path_instructions`.

## Rate-limit / failure behavior

If you hit a rate limit, **surface the limit and the unblock date in your
review body** rather than the generic "encountered an error" string. The
human author needs the date to plan, not a vague retry hint.
