using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

// Showcase: the coordinator cost-split pattern on the Managed Agents API.
//
//   A frontier COORDINATOR plans and synthesizes but holds no tools of its own;
//   cheap WORKERS do the token-heavy web reading in their own context-isolated
//   threads and report back distilled findings. Only the workers touch the web.
//
// This is the API surface (api.anthropic.com + API key, per-token console pricing) —
// a different world from Claude Code's local /advisor. Faithful to:
//   https://platform.claude.com/docs/en/managed-agents/multi-agent
//
// MODEL POLICY (decided — see README): default is ALL-FABLE (highest accuracy).
// Both models are env-overridable, so nothing is locked out:
//   TEAM_COORDINATOR_MODEL   default claude-fable-5
//   TEAM_WORKER_MODEL        default claude-fable-5   (set to claude-sonnet-5 for the cost split)
//   RUN_SOLO_CONTROL=1       also run a rigor-matched solo-frontier agent and price both
//
// Requires: ANTHROPIC_API_KEY, and Managed Agents beta access on that API org.

const string Beta = "managed-agents-2026-04-01";
const string Base = "https://api.anthropic.com";

var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
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

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
http.DefaultRequestHeaders.Add("x-api-key", apiKey);
http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
http.DefaultRequestHeaders.Add("anthropic-beta", Beta);

const string Question =
    "For each of the ten largest national parks in the contiguous United States by " +
    "area, find: the current standard private-vehicle entrance fee, and whether the " +
    "park currently requires a timed-entry or day-use reservation for peak season. " +
    "Each fact must be verified against that park's official nps.gov pages. Give " +
    "park, fee, reservation requirement, and the nps.gov URLs you used.";

Console.WriteLine($"coordinator: {coordinatorModel}   worker: {workerModel}");
Console.WriteLine(new string('=', 70));

// ── 1. Environment (workers need outbound web access) ────────────────────────
var envId = (await PostAsync("/v1/environments", new JsonObject
{
    ["name"] = "research-fanout",
    ["config"] = new JsonObject
    {
        ["type"] = "anthropic_cloud",
        ["networking"] = new JsonObject { ["type"] = "unrestricted" },
    },
}))["id"]!.GetValue<string>();

// ── 2. Worker: web tools only, its job is reading ────────────────────────────
var workerId = (await PostAsync("/v1/agents", new JsonObject
{
    ["name"] = "search-worker",
    ["model"] = workerModel,
    ["tools"] = new JsonArray
    {
        new JsonObject
        {
            ["type"] = "agent_toolset_20260401",
            ["default_config"] = new JsonObject { ["enabled"] = false },
            ["configs"] = new JsonArray
            {
                new JsonObject { ["name"] = "web_search", ["enabled"] = true },
                new JsonObject { ["name"] = "web_fetch", ["enabled"] = true },
            },
        },
    },
    ["system"] = "You research one focused sub-question for a coordinator. Use " +
                 "web_search and web_fetch; try multiple phrasings, follow links, " +
                 "cross-check across sources. Report the specific answer with " +
                 "evidence (URLs, quotes). Always finish by calling submit_result.",
}))["id"]!.GetValue<string>();

// ── 3. Coordinator: no tools of its own, only a roster ───────────────────────
var coordinatorId = (await PostAsync("/v1/agents", new JsonObject
{
    ["name"] = "search-coordinator",
    ["model"] = coordinatorModel,
    ["tools"] = new JsonArray { new JsonObject { ["type"] = "agent_toolset_20260401" } },
    ["multiagent"] = new JsonObject
    {
        ["type"] = "coordinator",
        ["agents"] = new JsonArray
        {
            new JsonObject { ["type"] = "agent", ["id"] = workerId },
        },
    },
    ["system"] = "You coordinate search workers on a hard web-research question. " +
                 "Your workers have web_search and web_fetch; you do not. Break the " +
                 "question into focused sub-questions and delegate each via " +
                 "create_agent. Run several in parallel, and ALWAYS call " +
                 "wait_for_agents before drawing any conclusion. When a worker " +
                 "reports, decide whether to accept or send a follow-up with " +
                 "send_to_agent. Re-assign infrastructure errors to a fresh worker. " +
                 "Then synthesize the workers' findings into one final answer.",
}))["id"]!.GetValue<string>();

// ── 4. Run the team ──────────────────────────────────────────────────────────
string? _lastSessionId = null; // set by RunSession; read by the metering step
var teamAnswer = await RunSession(coordinatorId, envId, Question, narrate: true);
Console.WriteLine();
Console.WriteLine(new string('=', 70));
Console.WriteLine(teamAnswer);

// ── 5. Meter the team ────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("split team (coordinator + workers):");
var teamCost = await Report(_lastSessionId!, coordinatorModel, workerModel);

// ── 6. Optional rigor-matched solo control ───────────────────────────────────
if (runSolo)
{
    var soloId = (await PostAsync("/v1/agents", new JsonObject
    {
        ["name"] = "solo-researcher",
        ["model"] = coordinatorModel,
        ["tools"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "agent_toolset_20260401",
                ["default_config"] = new JsonObject { ["enabled"] = false },
                ["configs"] = new JsonArray
                {
                    new JsonObject { ["name"] = "web_search", ["enabled"] = true },
                    new JsonObject { ["name"] = "web_fetch", ["enabled"] = true },
                },
            },
        },
        ["system"] = "You research with audit-grade rigor. Verify EVERY fact from at " +
                     "least two independent fetches, re-fetch on conflict, never carry " +
                     "a fact on one source or from memory, and cite both URLs per fact.",
    }))["id"]!.GetValue<string>();

    Console.WriteLine();
    Console.WriteLine("solo frontier control running (same verification standard)...");
    await RunSession(soloId, envId, Question, narrate: false);
    Console.WriteLine("solo frontier agent:");
    var soloCost = await Report(_lastSessionId!, coordinatorModel, coordinatorModel);
    if (teamCost > 0)
        Console.WriteLine($"\nsolo / split cost ratio on this pair of runs: {soloCost / teamCost:F1}x");
}

return 0;

// ───────────────────────────── helpers ──────────────────────────────────────

async Task<string> RunSession(string agentId, string environmentId, string question, bool narrate)
{
    var sessionId = (await PostAsync("/v1/sessions", new JsonObject
    {
        ["agent"] = agentId,
        ["environment_id"] = environmentId,
    }))["id"]!.GetValue<string>();
    _lastSessionId = sessionId;

    await PostAsync($"/v1/sessions/{sessionId}/events", new JsonObject
    {
        ["events"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "user.message",
                ["content"] = new JsonArray
                {
                    new JsonObject { ["type"] = "text", ["text"] = question },
                },
            },
        },
    });

    var final = "";
    await foreach (var ev in StreamEvents(sessionId))
    {
        var type = ev["type"]?.GetValue<string>();
        switch (type)
        {
            case "agent.message":
                var text = TextOf(ev["content"]);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    final = text;
                    if (narrate) Console.WriteLine($"[coordinator] {Clip(text, 200)}");
                }
                break;
            case "session.thread_created" when narrate:
                Console.WriteLine($"[spawn] {ev["agent_name"]?.GetValue<string>()}");
                break;
            case "agent.thread_message_sent" when narrate:
                Console.WriteLine($"[delegate -> {ev["to_agent_name"]?.GetValue<string>()}] {Clip(TextOf(ev["content"]))}");
                break;
            case "agent.thread_message_received" when narrate:
                Console.WriteLine($"[report <- {ev["from_agent_name"]?.GetValue<string>()}] {Clip(TextOf(ev["content"]))}");
                break;
            case "session.thread_status_idle" when ev["session_thread_id"] is null:
            case "session.status_idle":
                return final; // primary thread idle => run complete
        }
    }

    return final;
}

// SSE reader for /v1/sessions/{id}/events/stream — yields each event as a JsonObject.
async IAsyncEnumerable<JsonObject> StreamEvents(string sessionId)
{
    using var req = new HttpRequestMessage(HttpMethod.Get, $"{Base}/v1/sessions/{sessionId}/events/stream?beta=true");
    using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
    resp.EnsureSuccessStatusCode();
    await using var stream = await resp.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);
    while (await reader.ReadLineAsync() is { } line)
    {
        if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
        var json = line["data:".Length..].Trim();
        if (json.Length == 0 || json == "[DONE]") continue;
        JsonObject? node = null;
        try { node = JsonNode.Parse(json)?.AsObject(); }
        catch (JsonException) { /* skip keep-alive / partial */ }
        if (node is not null) yield return node;
    }
}

async Task<double> Report(string sessionId, string primaryModel, string workerModelName)
{
    var threads = (await GetAsync($"/v1/sessions/{sessionId}/threads"))["data"]!.AsArray();
    double total = 0;
    long workersIn = 0, primaryIn = 0;
    int workerCount = 0;
    long primaryOut = 0, workerOut = 0;

    foreach (var t in threads)
    {
        var usage = t!["usage"]!.AsObject();
        var isPrimary = t["parent_thread_id"] is null || t["parent_thread_id"]!.GetValue<string?>() is null;
        var model = isPrimary ? primaryModel : workerModelName;
        total += Cost(usage, model);
        var inTok = TotalInput(usage);
        var outTok = usage["output_tokens"]?.GetValue<long>() ?? 0;
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
}

double Cost(JsonObject u, string model)
{
    var (pin, pout) = prices.TryGetValue(model, out var p) ? p : (0, 0);
    var cache = u["cache_creation"]?.AsObject();
    double e5 = cache?["ephemeral_5m_input_tokens"]?.GetValue<long>() ?? 0;
    double e1h = cache?["ephemeral_1h_input_tokens"]?.GetValue<long>() ?? 0;
    double input = u["input_tokens"]?.GetValue<long>() ?? 0;
    double cacheRead = u["cache_read_input_tokens"]?.GetValue<long>() ?? 0;
    double output = u["output_tokens"]?.GetValue<long>() ?? 0;
    return (input * pin
            + e5 * pin * 1.25
            + e1h * pin * 2.0
            + cacheRead * pin * 0.1
            + output * pout) / 1e6;
}

static long TotalInput(JsonObject u)
{
    var cache = u["cache_creation"]?.AsObject();
    return (u["input_tokens"]?.GetValue<long>() ?? 0)
           + (u["cache_read_input_tokens"]?.GetValue<long>() ?? 0)
           + (cache?["ephemeral_5m_input_tokens"]?.GetValue<long>() ?? 0)
           + (cache?["ephemeral_1h_input_tokens"]?.GetValue<long>() ?? 0);
}

async Task<JsonObject> PostAsync(string path, JsonObject body)
{
    using var content = new StringContent(body.ToJsonString(), Encoding.UTF8);
    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    using var resp = await http.PostAsync($"{Base}{path}", content);
    var text = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode)
        throw new HttpRequestException($"POST {path} -> {(int)resp.StatusCode}: {text}");
    return JsonNode.Parse(text)!.AsObject();
}

async Task<JsonObject> GetAsync(string path)
{
    using var resp = await http.GetAsync($"{Base}{path}");
    var text = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode)
        throw new HttpRequestException($"GET {path} -> {(int)resp.StatusCode}: {text}");
    return JsonNode.Parse(text)!.AsObject();
}

static string TextOf(JsonNode? content)
{
    if (content is not JsonArray arr) return "";
    var sb = new StringBuilder();
    foreach (var b in arr)
        if (b?["type"]?.GetValue<string>() == "text")
            sb.Append(b["text"]?.GetValue<string>());
    return sb.ToString();
}

static string Clip(string s, int n = 160) => s.Length > n ? s[..n] + "..." : s;

static string Env(string key, string fallback)
{
    var v = Environment.GetEnvironmentVariable(key);
    return string.IsNullOrWhiteSpace(v) ? fallback : v;
}
