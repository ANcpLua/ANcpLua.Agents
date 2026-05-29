# ANcpLua.Agents.Hosting.GoogleGemini

Consumer toolkit for Microsoft Agent Framework — Qyl-prefixed facades over the
official `Google.GenAI` SDK. Wraps Gemini's `RawRepresentationFactory` +
`GenerateContentConfig` boilerplate as named extension methods on
`ChatClientAgentOptions`, plus a single-call agent factory.

Compatible with: Microsoft.Agents.AI 1.8.x
Tested against: Microsoft.Agents.AI 1.8.0
Capability tested against: Google.GenAI 1.6.2

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).

Channel: stable. The package depends only on stable `Microsoft.Agents.AI` and the
stable `Google.GenAI` SDK — no MAF preview/RC/alpha references.

## Why this package

Gemini's provider-specific tools (web search, maps, code execution) and thinking
configuration are not exposed through MEAI's generic `Hosted*Tool` types. They
live behind Google.GenAI's `GenerateContentConfig.Tools` and `ThinkingConfig`,
which are wired through a per-call `RawRepresentationFactory` lambda. Every
consumer that wants these features writes the same five-line lambda:

```csharp
ChatOptions = new ChatOptions
{
    RawRepresentationFactory = _ => new GenerateContentConfig
    {
        Tools = [ new Tool { GoogleSearch = new GoogleSearch { SearchTypes = new SearchTypes { WebSearch = new WebSearch() } } } ]
    }
}
```

The `WithQylGemini*` extensions collapse that lambda into named one-liners that
compose cleanly: multiple calls layer onto the same `GenerateContentConfig`
rather than overwriting each other.

## Quick start

```csharp
using ANcpLua.Agents.Hosting.GoogleGemini;
using Google.GenAI;

Client client = new(apiKey: "<your-key>");

ChatClientAgent agent = client.AsQylGeminiAgent("gemini-3-flash-preview");
AgentResponse response = await agent.RunAsync("What is the capital of France?");
Console.WriteLine(response);
```

## Web search (Gemini-specific grounding)

```csharp
using ANcpLua.Agents.Hosting.GoogleGemini;
using Google.GenAI;
using Microsoft.Agents.AI;

ChatClientAgentOptions options = new ChatClientAgentOptions()
    .WithQylGeminiWebSearch();

ChatClientAgent agent = client.AsQylGeminiAgent("gemini-3-flash-preview", options);
AgentResponse response = await agent.RunAsync("What is today's Space news?");
```

Distinct from MEAI's generic `HostedWebSearchTool` — this path activates
Gemini's provider-specific grounding behaviour. Google bills 5,000 prompts per
month free, then $14 / 1,000 search queries (as of this writing).

## Google Maps grounding (with optional widget)

```csharp
ChatClientAgentOptions options = new ChatClientAgentOptions()
    .WithQylGeminiMaps(enableWidget: true);

ChatClientAgent agent = client.AsQylGeminiAgent("gemini-3-flash-preview", options);
AgentResponse response = await agent.RunAsync(
    "What are the opening times of Hard Rock Cafe in New York? Do they have wheelchair access?");

// Pull grounding metadata + widget context token off the raw response
if (response.RawRepresentation is ChatResponse { RawRepresentation: GenerateContentResponse raw })
{
    foreach (Candidate candidate in raw.Candidates ?? [])
    {
        string? widgetToken = candidate.GroundingMetadata?.GoogleMapsWidgetContextToken;
        // Pass widgetToken to the Google Maps JavaScript API to render the widget.
    }
}
```

## Code execution (server-side sandbox)

```csharp
ChatClientAgentOptions options = new ChatClientAgentOptions()
    .WithQylGeminiCodeExecution();

ChatClientAgent agent = client.AsQylGeminiAgent("gemini-3-flash-preview", options);
AgentResponse response = await agent.RunAsync(
    "Make a chart of the top 5 countries by cars produced per year. Use CodeExecution to compute and plot.");

// Executable code + result are surfaced on the raw response
if (response.RawRepresentation is ChatResponse { RawRepresentation: GenerateContentResponse raw })
{
    Console.WriteLine(raw.ExecutableCode);
    Console.WriteLine(raw.CodeExecutionResult);
}
```

## Thinking configuration (Gemini 3+)

```csharp
ChatClientAgentOptions options = new ChatClientAgentOptions()
    .WithQylGeminiThinking(ThinkingLevel.High, includeThoughts: true);

ChatClientAgent agent = client.AsQylGeminiAgent("gemini-3-pro-preview", options);
AgentResponse response = await agent.RunAsync("Why is the sky blue?");

Console.WriteLine($"Reasoning tokens: {response.Usage!.ReasoningTokenCount}");

foreach (ChatMessage message in response.Messages)
foreach (AIContent content in message.Contents)
    if (content is TextReasoningContent reasoning)
        Console.WriteLine($"Reasoning: {reasoning.Text}");
```

`ThinkingLevel` replaces the older budget-token knob for Gemini 3+ models (Pro
supports `High`/`Low`; Flash supports `High`/`Medium`/`Low`/`Minimal`).

## Composing multiple options

`WithQylGemini*` extensions layer rather than overwrite, so a single options
chain can stack several Gemini features:

```csharp
ChatClientAgentOptions options = new ChatClientAgentOptions()
    .WithQylGeminiWebSearch()
    .WithQylGeminiCodeExecution()
    .WithQylGeminiThinking(ThinkingLevel.High, includeThoughts: true);

ChatClientAgent agent = client.AsQylGeminiAgent("gemini-3-pro-preview", options);
```
