using System.ComponentModel;
using ANcpLua.Agents.Governance;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Showcase: multi-agent spawn governance. A parent agent spawns child agents and the
// ANcpLua.Agents bounded-autonomy primitives enforce depth / spawn / tool-call limits while
// emitting human-readable lineage summaries. Fully offline over FakeChatClient.
//
// Combination: MAF AIAgent x ANcpLua.Agents.Instrumentation.QylAgentFactory x ANcpLua.Agents.Governance
//   (AgentCallLineage.TryEnter/Current/Complete/FormatLineageSummary,
//    AgentSpawnTracker.Register/GetDescendantCount + AgentSpawnLimitExceededException,
//    AgentCallGuard.FromEnvironment/Wrap/RecordCall)
// x ANcpLua.Agents.Testing.ChatClients.FakeChatClient.

// FormatLineageSummary's "/N" denominator reads ANCPLUA_AGENT_MAX_DEPTH (default 3); pin it so
// the printed summary matches the limits this showcase actually enforces.
Environment.SetEnvironmentVariable("ANCPLUA_AGENT_MAX_DEPTH", "3");

// ---------------------------------------------------------------------------------------------
// Section A — AgentCallLineage: a root (parent) agent and its spawn budget.
// AsyncLocal lineage threads through the call chain. We enter the root once, then spawn depth-1
// children iteratively; with maxSpawns: 3 the 4th spawn is refused.
// ---------------------------------------------------------------------------------------------
Console.WriteLine("== Section A: AgentCallLineage spawn budget ==");

using var childClient = new FakeChatClient();
childClient
    .WithResponse("child agent: researched one sub-topic.")
    .WithResponse("child agent: researched one sub-topic.")
    .WithResponse("child agent: researched one sub-topic.");

AIAgent researcher = QylAgentFactory.Create(childClient, options => options
    .WithName("researcher")
    .WithDescription("A child agent spawned to research a single sub-topic.")
    .WithInstructions("Research the requested sub-topic and report back one finding."));

var rootEntry = AgentCallLineage.TryEnter(maxDepth: 3, maxSpawns: 3);
if (!rootEntry.IsAllowed)
    throw new InvalidOperationException(rootEntry.RefusalReason);

AgentCallLineage root = rootEntry.Lineage!;
Console.WriteLine($"  [root]  {root.FormatLineageSummary()}");

for (var i = 1; i <= 4; i++)
{
    var childEntry = AgentCallLineage.TryEnter(maxDepth: 3, maxSpawns: 3);
    if (!childEntry.IsAllowed)
    {
        Console.WriteLine($"  spawn #{i} REFUSED -> {childEntry.RefusalReason}");
        continue;
    }

    AgentCallLineage child = childEntry.Lineage!;
    Console.WriteLine($"  spawn #{i} ALLOWED. Current session: {AgentCallLineage.Current?.SessionId}");
    Console.WriteLine($"    {child.FormatLineageSummary()}");

    var reply = await researcher.RunAsync($"Research sub-topic {i}.");
    Console.WriteLine($"    reply: {reply.Text}");

    // Complete restores the parent (root) lineage; spawn count is cumulative by design,
    // which is exactly why the 4th spawn trips the budget.
    child.Complete();
}

root.Complete();
Console.WriteLine();

// ---------------------------------------------------------------------------------------------
// Section B — AgentSpawnTracker: depth limit + descendant count.
// Pure linear nesting (no AsyncLocal). AgentToolPolicy.MaxAttempts is the depth cap, so a
// grandchild at depth 3 throws AgentSpawnLimitExceededException.
// ---------------------------------------------------------------------------------------------
Console.WriteLine("== Section B: AgentSpawnTracker depth limit ==");

var tracker = new AgentSpawnTracker();
// MaxAttempts doubles as the spawn DEPTH cap inside AgentSpawnTracker.
var policy = new AgentToolPolicy(MaxAttempts: 2, MaxToolCalls: 10, RequiredCapabilities: []);

const string RootRun = "run-root";
var rootCtx = tracker.Register(RootRun, parentRunId: null, policy);
Console.WriteLine($"  registered {RootRun} (depth {rootCtx.Depth})");

var childCtx = tracker.Register("run-child", parentRunId: RootRun, policy);
Console.WriteLine($"  registered run-child (depth {childCtx.Depth})");

var grandchildCtx = tracker.Register("run-grandchild", parentRunId: "run-child", policy);
Console.WriteLine($"  registered run-grandchild (depth {grandchildCtx.Depth})");

try
{
    // depth 3 > MaxAttempts (2): this spawn is rejected.
    tracker.Register("run-great-grandchild", parentRunId: "run-grandchild", policy);
}
catch (AgentSpawnLimitExceededException ex)
{
    Console.WriteLine($"  spawn REFUSED -> {ex.LimitKind} limit {ex.Limit}, attempted {ex.Actual}");
}

Console.WriteLine($"  descendants under {RootRun}: {tracker.GetDescendantCount(RootRun)}");
Console.WriteLine();

// ---------------------------------------------------------------------------------------------
// Section C — AgentCallGuard: per-call tool-invocation cap.
// (1) Wrap an AIFunction so the agent's own tool loop is auto-recorded.
// (2) Drive RecordCall directly against a low FromEnvironment cap until it trips.
// ---------------------------------------------------------------------------------------------
Console.WriteLine("== Section C: AgentCallGuard tool-call cap ==");

var wrapGuard = new AgentCallGuard(maxToolCalls: 10);
// Name the function explicitly: a top-level local function compiles to a mangled name, so we
// pin "search" and seed the matching FunctionCallContent below.
AIFunction guardedSearch = wrapGuard.Wrap(AIFunctionFactory.Create(SearchAsync, name: "search"));

using var toolClient = new FakeChatClient();
toolClient
    .WithResponse(
        [new FunctionCallContent("call_1", "search", new Dictionary<string, object?> { ["query"] = "lineage" })],
        ChatFinishReason.ToolCalls)
    .WithResponse("Parent agent finished: 1 search performed.");

AIAgent searchAgent = QylAgentFactory.Create(toolClient, options => options
    .WithName("search-agent")
    .WithDescription("Parent agent that calls a guarded search tool.")
    .WithInstructions("Use the search tool to answer the question.")
    .WithTools([guardedSearch]));

var searchReply = await searchAgent.RunAsync("Find references to lineage.");
Console.WriteLine($"  agent: {searchReply.Text}");
Console.WriteLine($"  guard recorded {wrapGuard.TotalCalls} tool call(s): " +
    string.Join(", ", wrapGuard.ToolCallCounts.Select(kv => $"{kv.Key}={kv.Value}")));

// Drive RecordCall directly against a tiny cap; the cap is inclusive so the 3rd call trips.
var capGuard = AgentCallGuard.FromEnvironment(defaultMax: 3);
Console.WriteLine($"  hard cap: {capGuard.MaxToolCalls} tool calls");
try
{
    for (var i = 1; i <= 5; i++)
    {
        capGuard.RecordCall("search");
        Console.WriteLine($"  RecordCall #{i} OK ({capGuard.TotalCalls}/{capGuard.MaxToolCalls})");
    }
}
catch (OperationCanceledException ex)
{
    Console.WriteLine($"  cap reached -> {ex.Message.Split('\n')[0]}");
}

static Task<string> SearchAsync(
    [Description("Search query.")] string query,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"results for '{query}': 1 hit");
}
