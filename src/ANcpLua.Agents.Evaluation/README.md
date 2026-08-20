# ANcpLua.Agents.Evaluation

A fluent, **fail-closed** evaluation suite for Microsoft Agent Framework agents. One builder —
`EvaluationSuite` — composes deterministic local checks, LLM-judged quality, and any other
`IAgentEvaluator` stage over a **single** agent run, then ends in a hard gate: the process exits
non-zero unless there is positive, green evidence for every evaluated item.

Compatible with: Microsoft.Agents.AI 1.13.x
Tested against: Microsoft.Agents.AI 1.13.0

Channel: stable. This package must not reference Microsoft Agent Framework preview, RC, or alpha packages.

## Surface

```csharp
return await EvaluationSuite.Create("pr-gate")
    .Agent(agent, "Give me a one-day itinerary for Paris.")
    .Expecting("Eiffel Tower")                                     // ground truth, one per query
    .Check(EvalChecks.NonEmpty(), EvalChecks.ContainsExpected())    // deterministic, no model
    .ExpectTools(ToolExpectation.AllOf("get_weather"))              // red until the agent really calls it
    .Custom("no_refusal", item =>                                   // your own rule, with a reason
        (!item.Response.Contains("I can't help"), "no refusal language"))
    .Quality(new RelevanceEvaluator(), judge, minScore: 4.0)        // LLM-judged, thresholded
    .GateAsync();   // prints the breakdown; 0 only if every stage ran and every item passed
```

`RunAsync()` returns the `SuiteReport` instead, for callers that want the raw verdict object;
`report.AssertOrThrow()` is the entry point for a test framework and names every failing item.

## Why fail-closed

The raw engine's default verdict is *pass = not-positively-failed*: a quality score with no
interpretation, a boolean check that never got a value, and a vacuous tool check all read as **pass**.
This package inverts that to *pass = positively asserted*:

- `Quality(...)` stamps a hard pass/fail from `minScore`, so a low or **missing** score fails closed.
- `ToolExpectation.WithArguments(...)` fails when there is nothing to compare against.
- `SuiteReport` classifies an un-scored, empty, or errored item as `Error` — never a pass.
- A requested stage that cannot run is an **error**, never a silent skip — and the sibling stages'
  verdicts still survive and still print.
- A check that *throws* costs one item, not the whole stage. `LocalEvaluator` invokes checks directly,
  so an exception out of one predicate would otherwise abort the batch and take every sibling verdict
  — including the real failures you needed to see — down with it.

A positively-asserted failure outranks an indeterminacy: an empty response that fails a `NonEmpty`
check is reported as `Fail`, not `Error`. Calling it an error would dress a genuine red up as a broken
harness.

## Stages this package does not depend on

`Stage(name, evaluator)` takes any `IAgentEvaluator` and runs it over the same single run, under the
same gate. That is the seam for server-side providers whose SDKs ship on a preview channel — Azure AI
Foundry's `FoundryEvals`, for example — so this package's own dependency closure stays stable:

```csharp
suite.Stage("foundry", new FoundryEvals(projectClient, model, FoundryEvals.Relevance));
```

## Custom checks

`Custom` takes a `bool` or a `(bool Passed, string Reason)` tuple — both convert to `CheckOutcome`.
Prefer the tuple: every built-in check reports a reason, and a bare `false` is the one verdict in the
report that cannot say why.

```csharp
.Custom("min_length", item => (item.Response.Length >= 100, $"{item.Response.Length} chars, wanted >= 100"))
```
