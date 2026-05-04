# Copilot PR-review instructions for ANcpLua.Agents

Microsoft Agent Framework (MAF) consumer toolkit. Ships three NuGet packages:
`ANcpLua.Agents` (governance, instrumentation, factory helpers), `ANcpLua.Agents.Testing`
(`FakeChatClient`, scripted clients, six provider conformance fixtures, SSE parsing),
and `ANcpLua.Agents.Testing.Workflows` (workflow fixture base, in-memory checkpoint
manager). All target `net10.0`. Pinned to `Microsoft.Agents.AI` 1.3.0 stable +
`Microsoft.Agents.AI.Workflows.Declarative` 1.3.0-rc1; `Microsoft.Extensions.AI` 10.5.0.
`AllowMrtsPrerelease=true` for MAF preview consumption. CPM enforced via
`Directory.Packages.props`. This file scopes to PR review only.

## Flag

- `IChatClient` instances created without a `using` block. `IChatClient` is
  `IDisposable`; OpenAI / Azure / Anthropic SDK clients hold HTTP connections,
  forgetting `Dispose()` leaks sockets under load.
- `AgentConcurrencyLimiter` constructed without `using` — it owns a
  `SemaphoreSlim` dictionary; not disposing leaks handles, and acquiring after
  dispose throws `ObjectDisposedException`.
- Mutating `ChatOptions.Tools` (or any `ChatOptions` field) on an instance shared
  across concurrent agent runs. `ToolDecoratingChatClient.PrepareOptions()`
  mutates in place; sharing a `ChatOptions` across `GetResponseAsync` calls
  produces inconsistent tool decorations or torn state.
- `await foreach` over a streaming response (`run.WatchStreamAsync()`,
  `agent.RunStreamingAsync()`) without a `CancellationToken` parameter or
  `.WithCancellation(ct)` chained. Streams are long-running; missing cancellation
  blocks shutdown.
- Hardcoded model names (`"gpt-4o"`, `"claude-3-5-sonnet"`) without an env-var
  override or options-driven path. The established pattern is
  `TryCreateFromEnvironment()` reading `ANCPLUA_AGENT_MODEL`.
- New `IChatClient` or `AIAgent` factory site missing the established telemetry
  wrap (consumer responsibility — wrappers like `.UseQylAgentTelemetry()` live in
  downstream consumers, but factory helpers here must not block them).

## agents-specific

- All three packages still ship from this repo (the
  `MAF.Advanced.Patterns` consolidation moves are paused). Don't suggest
  redirecting users to `MAF.Advanced.Patterns` until that migration completes.
- MAF version pin lives in `Directory.Packages.props`. Don't bump
  `Microsoft.Agents.AI` or `Microsoft.Extensions.AI` in a feature PR — version
  bumps are their own commits with downstream impact analysis.
- `LangVersion=preview` is on. C# 14 features (file-scoped namespaces, primary
  constructors, required init, `params ReadOnlySpan<T>`) are repo defaults.
- `ANcpLua.Agents.Testing.Workflows/` includes harvested copies of MAF 1.3.0
  internals (under `Internals/`). Don't refactor those toward "cleaner" shape —
  they exist verbatim for upstream parity, and `RS0030` (banned
  `ArgumentNullException.ThrowIfNull`) is suppressed there because upstream uses
  the banned API.

## Do not flag

- Allow-listed suppressions in `ANcpLua.Agents.Testing.csproj`: `CA1002`,
  `CA1034`, `CA1305`, `CA1707`, `CA1816`, `CA1819`, `CA1826`, `CA2000`, `CS1591`,
  `AL0014`, `AL0025`, `AL0131`, `IDE1006`, `IDE0060`, `IDE0059` — the file's own
  comment documents these as MAF-upstream-parity concessions.
- `MEAI001` suppression in `Testing.Workflows` (preview API marker for
  `Workflows.Declarative` rc1).
- `NU1604` in `Directory.Build.props` (CPM transitive-pin warning).
- `RS0030` inside `Internals/` directories (harvested upstream code).
- Missing XML doc comments on `internal` types or harvested upstream helpers.

## Project context

Solo-dev repo. The owner controls all known consumers (qyl primarily) and
handles downstream fixes in the same session. Breaking changes are allowed;
bump major and fix consumers. Don't suggest backwards-compat shims, feature
flags, or deprecated-marker dual paths within a single PR. Test infrastructure
(`FakeChatClient`, `MockChatClients`, `WorkflowFixture`, conformance suites for
OpenAI / Azure / Anthropic / Ollama / Google / OpenRouter) lives here today;
some of it is slated to migrate to `MAF.Advanced.Patterns.Testing` once the
qyl-consumption decision unblocks the consolidation.
