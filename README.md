# ANcpLua.Agents

Runtime helpers + test infrastructure for Microsoft Agent Framework (MAF) consumers.

## Packages

| Package | Purpose |
|---|---|
| `ANcpLua.Agents` | Runtime helpers — agent/workflow convenience wrappers, console rendering, extension methods on `AIAgent` / `ChatClientAgent`. |
| `ANcpLua.Agents.Testing` | Test surface — `FakeChatClient`, `MockChatClients` family, `IAgentFixture`, conformance suites (`RunTests<TFixture>`, `StructuredOutputRunTests<TFixture>`, …), 6 live-provider example fixtures. |
| `ANcpLua.Agents.Testing.Workflows` | Workflow test surface — `WorkflowFixture`, `WorkflowHarness`, `InMemoryJsonStore`, stateful session-serializing `Test*Agent` fakes. |

## Why a separate repo

- MAF churn (RC → GA cycles) should not hit the Roslyn foundation repo
- Consumers only needing Roslyn helpers don't pull `Microsoft.Agents.AI.*`, `OpenAI`, `Anthropic.SDK`, `Azure.AI.OpenAI`, `OllamaSharp`
- Test infrastructure can evolve independently of the foundation SDK release cadence

## Related

- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) — Roslyn foundation (netstandard2.0 helpers, source-only packages, polyfills, generator test engine)
- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) — MSBuild SDK shipping build conventions + centralized Version.props
- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) — Roslyn diagnostic analyzers (AL0001-AL013x)
