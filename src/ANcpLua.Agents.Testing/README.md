# ANcpLua.Agents.Testing

Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Stable test infrastructure for Microsoft Agent Framework consumers: fake chat clients, conformance bases, diagnostics, and provider fixtures for OpenAI, Azure OpenAI, Anthropic, Ollama, Google Gemini, and OpenRouter.
Run harnesses for focused, non-inherited assertions are available via `Harnesses/AgentRunHarness`.

Compatible with: Microsoft.Agents.AI 1.8.x
Tested against: Microsoft.Agents.AI 1.8.0

Channel: stable. This package keeps provider fixtures free of Microsoft Agent Framework preview, RC, and alpha dependencies.

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).

```csharp
var result = await AgentRunHarness.For(new FakeEchoAgent())
    .WithUserMessage("hello")
    .RunAsync();

result.Should().HaveTextContaining("hello");
```
