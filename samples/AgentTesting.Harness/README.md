# AgentTesting.Harness

An offline xUnit v3 test project that showcases the ANcpLua agent + workflow **testing toolkit**
(`ANcpLua.Agents.Testing` and `ANcpLua.Agents.Testing.Workflows`) driving Microsoft Agent Framework
(MAF) agents and workflows with **no API keys** — every agent runs over an in-memory
`FakeChatClient` and every workflow runs in the in-process execution environment.

What it demonstrates (one combination per `[Fact]`):

- **AgentRunHarness over a `FakeChatClient` agent** — a `ChatClientAgent` is built with the
  `QylAgentOptionsBuilder` facade over a seeded `FakeChatClient`, then driven through
  `AgentRunHarness.For(agent).WithUserMessage(...).RunAsync()`. The materialized
  `AgentRunHarnessResult` is asserted with `Should().HaveTextContaining(...)`. A second fact uses
  `.RunStreamingAsync()` and asserts the concatenated streaming text.
- **`ActivityCollector` capturing an agent span** — the agent is wrapped with
  `UseAgentRunTelemetry`, an `ActivityCollector` listens on the configured `ActivitySource`, and the
  captured `agent.run` span is checked with the `ActivityAssert` fluent extensions
  (`AssertKind` / `AssertTag` / `AssertStatus`) including the `gen_ai.*` semantic-convention tags.
- **`WorkflowFixture<TInput>` running a tiny workflow** — a subclass overrides `BuildWorkflow()` to
  build a single-executor MAF workflow (an `Executor<string,string>` that uppercases its input and
  calls `context.YieldOutputAsync(...)`, wired with `WithOutputFrom`). `RunAsync(input)` materializes
  a `WorkflowRunResult` asserted with `Should().HaveNoErrors().And.YieldOutput<string>(...)`.

## Run

```bash
cd /Users/ancplua/ANcpLua.Agents
dotnet test samples/AgentTesting.Harness/AgentTesting.Harness.csproj -c Debug
```
