using Anthropic;
using Anthropic.Models.Beta.Agents;
using Anthropic.Models.Beta.Environments;
using Anthropic.Models.Beta.Sessions;
using Anthropic.Models.Beta.Sessions.Events;
using Anthropic.Models.Beta.Sessions.Threads;

// Showcase: the coordinator cost-split pattern on the Managed Agents API,
// built on the official Anthropic .NET SDK (no hand-rolled transport).
//
//   A frontier COORDINATOR plans and synthesizes but holds no tools of its own;
//   cheap WORKERS do the token-heavy web reading in their own context-isolated
//   threads and report back distilled findings. Only the workers touch the web.
//
// This is the API surface (api.anthropic.com + API key, per-token console pricing) —
// a different world from Claude Code's local /advisor. The SDK sets the
// managed-agents-2026-04-01 beta header automatically. Faithful to:
//   https://platform.claude.com/docs/en/managed-agents/multi-agent
//
// MODEL POLICY (decided — see README): default is ALL-FABLE (highest accuracy).
// Both models are env-overridable, so nothing is locked out:
//   TEAM_COORDINATOR_MODEL   default claude-fable-5
//   TEAM_WORKER_MODEL        default claude-fable-5   (set claude-sonnet-5 for the split)
//   RUN_SOLO_CONTROL=1       also run a rigor-matched solo-frontier agent and price both
//
// Requires: ANTHROPIC_API_KEY (an API-org key), with Managed Agents beta access.

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
{
    Console.Error.WriteLine("Set ANTHROPIC_API_KEY (an API-org key, not your Claude Max login).");
    return 1;
}

var coordinatorModel = Env("TEAM_COORDINATOR_MODEL", "claude-fable-5");
var workerModel = Env("TEAM_WORKER_MODEL", "claude-fable-5");
var runSolo = Environment.GetEnvironmentVariable("RUN_SOLO_CONTROL") == "1";

// $/MTok (input, output) from the pricing page — update as rates change.
// Sonnet 5 shown at its introductory rate ($2/$10 through 2026-08-31; $3/$15 after).
var prices = new Dictionary<string, (double In, double Out)>
{
    ["claude-fable-5"] = (10.0, 50.0),
    ["claude-sonnet-5"] = (2.0, 10.0),
    ["claude-opus-4-8"] = (5.0, 25.0),
};

using var client = new AnthropicClient(); // reads ANTHROPIC_API_KEY from the environment

const string Question =
    "For each of the ten largest national parks in the contiguous United States by " +
    "area, find: the current standard private-vehicle entrance fee, and whether the " +
    "park currently requires a timed-entry or day-use reservation for peak season. " +
    "Each fact must be verified against that park's official nps.gov pages. Give " +
    "park, fee, reservation requirement, and the nps.gov URLs you used.";

Console.WriteLine($"coordinator: {coordinatorModel}   worker: {workerModel}");
Console.WriteLine(new string('=', 70));

// ── 1. Environment (workers need outbound web access) ────────────────────────
var environment = await client.Beta.Environments.Create(new EnvironmentCreateParams
{
    Name = "research-fanout",
    Config = new BetaCloudConfigParams { Networking = new BetaUnrestrictedNetwork() },
});
var envId = environment.ID;

// ── 2. Worker: web tools only, its job is reading ────────────────────────────
// Scoping to web_search + web_fetch is also the security boundary — workers read
// untrusted web pages, so that is the blast radius you want for that input.
BetaManagedAgentsAgentToolset20260401Params WebToolsOnly() => new()
{
    Type = "agent_toolset_20260401",
    DefaultConfig = new BetaManagedAgentsAgentToolsetDefaultConfigParams { Enabled = false },
    Configs =
    [
        new BetaManagedAgentsAgentToolConfigParams { Name = "web_search", Enabled = true },
        new BetaManagedAgentsAgentToolConfigParams { Name = "web_fetch", Enabled = true },
    ],
};

var worker = await client.Beta.Agents.Create(new AgentCreateParams
{
    Name = "search-worker",
    Model = new BetaManagedAgentsModelConfigParams { ID = workerModel },
    Tools = [WebToolsOnly()],
    System = "You research one focused sub-question for a coordinator. Use web_search " +
             "and web_fetch; try multiple phrasings, follow links, cross-check across " +
             "sources. Report the specific answer with evidence (URLs, quotes). Always " +
             "finish by calling submit_result.",
});

// ── 3. Coordinator: no tools of its own, only a roster ───────────────────────
var coordinator = await client.Beta.Agents.Create(new AgentCreateParams
{
    Name = "search-coordinator",
    Model = new BetaManagedAgentsModelConfigParams { ID = coordinatorModel },
    Tools = [new BetaManagedAgentsAgentToolset20260401Params { Type = "agent_toolset_20260401" }],
    Multiagent = new BetaManagedAgentsMultiagentParams
    {
        Type = "coordinator",
        Agents = [worker.ID], // roster entry accepts a bare agent id
    },
    System = "You coordinate search workers on a hard web-research question. Your " +
             "workers have web_search and web_fetch; you do not. Break the question into " +
             "focused sub-questions and delegate each via create_agent. Run several in " +
             "parallel, and ALWAYS call wait_for_agents before drawing any conclusion. " +
             "When a worker reports, decide whether to accept or send a follow-up with " +
             "send_to_agent. Re-assign infrastructure errors to a fresh worker. Then " +
             "synthesize the workers' findings into one final answer.",
});

// ── 4. Run the team ──────────────────────────────────────────────────────────
var (teamAnswer, teamSessionId) = await RunSession(coordinator.ID, envId, Question, narrate: true);
Console.WriteLine();
Console.WriteLine(new string('=', 70));
Console.WriteLine(teamAnswer);

// ── 5. Meter the team ────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("split team (coordinator + workers):");
var teamCost = await Report(teamSessionId, coordinatorModel, workerModel);

// ── 6. Optional rigor-matched solo control ───────────────────────────────────
if (runSolo)
{
    var solo = await client.Beta.Agents.Create(new AgentCreateParams
    {
        Name = "solo-researcher",
        Model = new BetaManagedAgentsModelConfigParams { ID = coordinatorModel },
        Tools = [WebToolsOnly()],
        System = "You research with audit-grade rigor. Verify EVERY fact from at least " +
                 "two independent fetches, re-fetch on conflict, never carry a fact on " +
                 "one source or from memory, and cite both URLs per fact.",
    });

    Console.WriteLine();
    Console.WriteLine("solo frontier control running (same verification standard)...");
    var (_, soloSessionId) = await RunSession(solo.ID, envId, Question, narrate: false);
    Console.WriteLine("solo frontier agent:");
    var soloCost = await Report(soloSessionId, coordinatorModel, coordinatorModel);
    if (teamCost > 0)
        Console.WriteLine($"\nsolo / split cost ratio on this pair of runs: {soloCost / teamCost:F1}x");
}

return 0;

// ───────────────────────────── helpers ──────────────────────────────────────

async Task<(string Answer, string SessionId)> RunSession(string agentId, string environmentId, string question, bool narrate)
{
    var session = await client.Beta.Sessions.Create(new SessionCreateParams
    {
        Agent = agentId,
        EnvironmentID = environmentId,
    });

    await client.Beta.Sessions.Events.Send(session.ID, new EventSendParams
    {
        Events =
        [
            new BetaManagedAgentsUserMessageEventParams
            {
                Type = "user.message",
                Content = [new BetaManagedAgentsTextBlock { Type = "text", Text = question }],
            },
        ],
    });

    var final = "";
    await foreach (var ev in client.Beta.Sessions.Events.StreamStreaming(session.ID))
    {
        switch (ev.Value)
        {
            case BetaManagedAgentsAgentMessageEvent m:
                var text = string.Concat(m.Content.Select(b => b.Text));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    final = text;
                    if (narrate) Console.WriteLine($"[coordinator] {Clip(text, 200)}");
                }
                break;
            case BetaManagedAgentsSessionThreadCreatedEvent when narrate:
                Console.WriteLine($"[spawn] {ev.AgentName}");
                break;
            case BetaManagedAgentsSessionStatusIdleEvent:
                return (final, session.ID); // whole session idle => run complete
        }
    }

    return (final, session.ID);
}

async Task<double> Report(string sessionId, string primaryModel, string workerModelName)
{
    var threads = new List<BetaManagedAgentsSessionThread>();
    var page = await client.Beta.Sessions.Threads.List(sessionId);
    while (true)
    {
        threads.AddRange(page.Items);
        if (!page.HasNext()) break;
        page = await page.Next();
    }

    double total = 0;
    long workersIn = 0, primaryIn = 0, primaryOut = 0, workerOut = 0;
    var workerCount = 0;

    foreach (var t in threads)
    {
        if (t.Usage is not { } usage) continue;
        var isPrimary = string.IsNullOrEmpty(t.ParentThreadID);
        var model = isPrimary ? primaryModel : workerModelName;
        total += Cost(usage, model);
        var inTok = TotalInput(usage);
        var outTok = usage.OutputTokens ?? 0;
        if (isPrimary) { primaryIn = inTok; primaryOut = outTok; }
        else { workersIn += inTok; workerOut += outTok; workerCount++; }
    }

    Console.WriteLine($"  primary thread ({primaryModel}): {primaryIn,10:N0} in / {primaryOut,7:N0} out");
    if (workerCount > 0)
    {
        Console.WriteLine($"  {workerCount} worker(s) ({workerModelName}): {workersIn,10:N0} in / {workerOut,7:N0} out");
        var share = (double)workersIn / Math.Max(1, workersIn + primaryIn);
        Console.WriteLine($"  workers' share of input: {share:P0}");
    }
    Console.WriteLine($"  total cost: ${total:F2}");
    return total;

    double Cost(BetaManagedAgentsSessionThreadUsage u, string model)
    {
        var (pin, pout) = prices.TryGetValue(model, out var p) ? p : (0, 0);
        double input = u.InputTokens ?? 0;
        double cacheRead = u.CacheReadInputTokens ?? 0;
        double e5 = u.CacheCreation?.Ephemeral5mInputTokens ?? 0;
        double e1h = u.CacheCreation?.Ephemeral1hInputTokens ?? 0;
        double output = u.OutputTokens ?? 0;
        return (input * pin
                + e5 * pin * 1.25
                + e1h * pin * 2.0
                + cacheRead * pin * 0.1
                + output * pout) / 1e6;
    }
}

static long TotalInput(BetaManagedAgentsSessionThreadUsage u) =>
    (u.InputTokens ?? 0)
    + (u.CacheReadInputTokens ?? 0)
    + (u.CacheCreation?.Ephemeral5mInputTokens ?? 0)
    + (u.CacheCreation?.Ephemeral1hInputTokens ?? 0);

static string Clip(string s, int n = 160) => s.Length > n ? s[..n] + "..." : s;

static string Env(string key, string fallback)
{
    var v = Environment.GetEnvironmentVariable(key);
    return string.IsNullOrWhiteSpace(v) ? fallback : v;
}
