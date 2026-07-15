// Showcase: automated incident triage + remediation as a *correct* MAF Workflows graph, fully offline.
//
// Combination:
//   MAF Microsoft.Agents.AI.Workflows (Executor / ProtocolBuilder / RouteBuilder / conditioned edges /
//     IResettableExecutor / custom WorkflowEvent / InProcessExecution)
//   x MAF structured output (AIAgent.RunAsync<T> -> AgentResponse<T>)
//   x ANcpLua.Agents.Instrumentation (QylAgentFactory) over ANcpLua.Agents.Testing FakeChatClient (no API keys).
//
// This is the corrected form of a common "inline-routing inside executors + agents-as-graph-nodes"
// sketch. Five things that sketch gets wrong, fixed here:
//   1. Routing lives on the EDGES (AddEdge<T>(src, tgt, condition)), not as an if/else that calls
//      SendMessageAsync the same way on every branch (which just broadcasts to every out-edge).
//   2. Agents are wrapped INSIDE executors that call agent.RunAsync<T> for typed output, instead of
//      being graph nodes fed raw domain records — a workflow agent node only speaks the chat protocol
//      (List<ChatMessage> + TurnToken), so an IncidentSignal / SolutionPlan can't flow through one.
//   3. Diagnostics use a real WorkflowEvent subtype via AddEventAsync(WorkflowEvent), not an
//      anonymous object (AddEventAsync has no object overload).
//   4. Parameterless executors use the primary-constructor base call `X() : Executor("id")`.
//   5. Executors implement IResettableExecutor so the single Workflow instance can be safely reused
//      across runs (reusing a workflow with shared, non-resettable executors throws).
//
// Scenario: each incident is intake-counted, then severity-routed. critical/warning incidents go
// through the RCA agent -> Solution agent -> a policy review bridge that approves safe plans and
// rejects destructive ones; everything else is logged below the auto-remediation threshold.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

// Round-trippable options: the same instance generates the canned FakeChatClient JSON and is handed to
// RunAsync<T>, so the offline "model output" deserializes back into the exact record it came from.
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

// --- Offline agents. FakeChatClient returns canned JSON; RunAsync<T> sets a JSON-schema response
//     format (ignored by the fake) and deserializes the response text into the typed record. ---
using var rcaClient = new FakeChatClient();
rcaClient
    .WithResponse(JsonSerializer.Serialize(
        new RootCauseHypothesis("INC-1042", "Connection pool exhausted: requests queued past the 30s timeout during a traffic spike."), json))
    .WithResponse(JsonSerializer.Serialize(
        new RootCauseHypothesis("INC-1043", "A migration left an unindexed foreign key, forcing full table scans on the hot path."), json));

using var solutionClient = new FakeChatClient();
solutionClient
    .WithResponse(JsonSerializer.Serialize(
        new SolutionPlan("INC-1042", "Raise the pool max size and add a circuit breaker. Safe, reversible config change."), json))
    .WithResponse(JsonSerializer.Serialize(
        new SolutionPlan("INC-1043", "Run a destructive online rebuild that drops and recreates the index during peak traffic."), json));

AIAgent rcaAgent = QylAgentFactory.Create(rcaClient, options => options
    .WithName("rca-agent")
    .WithDescription("Reliability engineer that proposes a root-cause hypothesis.")
    .WithInstructions("Analyze the incident and return a concise root-cause hypothesis."));

AIAgent solutionAgent = QylAgentFactory.Create(solutionClient, options => options
    .WithName("solution-agent")
    .WithDescription("Remediation planner that proposes an execution plan.")
    .WithInstructions("Given a root-cause hypothesis, produce a remediation plan."));

// --- Build ONE workflow and reuse it across incidents (IResettableExecutor makes that safe). ---
var intake = new IntakeExecutor();
var rca = new RcaExecutor(rcaAgent, json);
var solution = new SolutionExecutor(solutionAgent, json);
var review = new ReviewBridgeExecutor();
var approvedSink = new OutputSinkExecutor("approved-sink", "✓ APPROVED");
var rejectedSink = new OutputSinkExecutor("rejected-sink", "✗ REJECTED");
var infoSink = new OutputSinkExecutor("info-sink", "ℹ INFO");

Workflow workflow = new WorkflowBuilder(intake)
    // Conditional routing on the edge, evaluated against the IncidentSignal the intake forwards.
    .AddEdge<IncidentSignal>(intake, rca, s => s is { Severity: "critical" or "warning" })
    .AddEdge<IncidentSignal>(intake, infoSink, s => s is not { Severity: "critical" or "warning" })
    // The diagnostic agent chain (each step is an executor that internally calls its agent).
    .AddEdge(rca, solution)
    .AddEdge(solution, review)
    // The review bridge routes its verdict by approval flag — again on the edge.
    .AddEdge<ConfidenceVerdict>(review, approvedSink, v => v is { Approved: true })
    .AddEdge<ConfidenceVerdict>(review, rejectedSink, v => v is { Approved: false })
    .WithOutputFrom(approvedSink, rejectedSink, infoSink)
    .Build();

IncidentSignal[] incidents =
[
    new("INC-1042", "checkout-api", "critical", "5xx rate spiked to 12% after the latest deploy."),
    new("INC-1043", "orders-db", "warning", "p99 query latency tripled overnight."),
    new("INC-2001", "marketing-site", "info", "Cache hit ratio dipped 2% during a campaign."),
];

foreach (IncidentSignal incident in incidents)
{
    Console.WriteLine($"── {incident.Id} [{incident.Severity}] {incident.Service} ──");

    List<WorkflowEvent> events = [];
    await using (StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, incident))
    {
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            events.Add(evt);
        }
    }

    foreach (IntakeObservedEvent observed in events.OfType<IntakeObservedEvent>())
    {
        Console.WriteLine($"   intake observed {observed.SignalId} ({observed.SeenThisRun} this run)");
    }

    string? outcome = events.OfType<WorkflowOutputEvent>()
        .Select(static evt => evt.As<string>())
        .FirstOrDefault(static text => text is not null);

    Console.WriteLine($"   → {outcome ?? "(no output)"}");
    Console.WriteLine();
}

// ---------------------------------------------------------------------------
// Workflow message contracts.
// ---------------------------------------------------------------------------

internal sealed record IncidentSignal(string Id, string Service, string Severity, string Description);

internal sealed record RootCauseHypothesis(string SignalId, string Hypothesis);

internal sealed record SolutionPlan(string SignalId, string Approach);

internal sealed record ConfidenceVerdict(string SignalId, bool Approved, string Reason);

/// <summary>Diagnostic event the intake emits upstream — a real <see cref="WorkflowEvent"/>, not an anonymous object.</summary>
internal sealed class IntakeObservedEvent(string signalId, int seenThisRun) : WorkflowEvent(seenThisRun)
{
    public string SignalId => signalId;

    public int SeenThisRun => seenThisRun;
}

// ---------------------------------------------------------------------------
// Executors. Routing is declared per-route in ConfigureProtocol; the graph topology (which executor
// feeds which) and the conditions live in the WorkflowBuilder above.
// ---------------------------------------------------------------------------

/// <summary>Counts signals seen in the current run, emits a diagnostic event, and forwards the signal.</summary>
internal sealed class IntakeExecutor() : Executor("intake"), IResettableExecutor
{
    private int _seenThisRun;

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        protocolBuilder.RouteBuilder.AddHandler<IncidentSignal>(this.ObserveAsync);
        return protocolBuilder.SendsMessage<IncidentSignal>();
    }

    private async ValueTask ObserveAsync(IncidentSignal signal, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _seenThisRun++;
        await context.AddEventAsync(new IntakeObservedEvent(signal.Id, _seenThisRun), cancellationToken);
        await context.SendMessageAsync(signal, cancellationToken: cancellationToken);
    }

    // Per-run state is zeroed on reuse, so a reused Workflow instance never leaks state between runs.
    public ValueTask ResetAsync()
    {
        _seenThisRun = 0;
        return default;
    }
}

/// <summary>Wraps the RCA agent: turns an <see cref="IncidentSignal"/> into a typed <see cref="RootCauseHypothesis"/>.</summary>
internal sealed class RcaExecutor(AIAgent agent, JsonSerializerOptions serializerOptions) : Executor("rca"), IResettableExecutor
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        protocolBuilder.RouteBuilder.AddHandler<IncidentSignal>(this.DiagnoseAsync);
        return protocolBuilder.SendsMessage<RootCauseHypothesis>();
    }

    private async ValueTask DiagnoseAsync(IncidentSignal signal, IWorkflowContext context, CancellationToken cancellationToken)
    {
        AgentResponse<RootCauseHypothesis> response = await agent.RunAsync<RootCauseHypothesis>(
            $"Incident {signal.Id} on {signal.Service} [{signal.Severity}]: {signal.Description}",
            serializerOptions: serializerOptions,
            cancellationToken: cancellationToken);

        await context.SendMessageAsync(response.Result, cancellationToken: cancellationToken);
    }

    public ValueTask ResetAsync() => default;
}

/// <summary>Wraps the remediation agent: turns a <see cref="RootCauseHypothesis"/> into a typed <see cref="SolutionPlan"/>.</summary>
internal sealed class SolutionExecutor(AIAgent agent, JsonSerializerOptions serializerOptions) : Executor("solution"), IResettableExecutor
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        protocolBuilder.RouteBuilder.AddHandler<RootCauseHypothesis>(this.PlanAsync);
        return protocolBuilder.SendsMessage<SolutionPlan>();
    }

    private async ValueTask PlanAsync(RootCauseHypothesis hypothesis, IWorkflowContext context, CancellationToken cancellationToken)
    {
        AgentResponse<SolutionPlan> response = await agent.RunAsync<SolutionPlan>(
            $"Root cause for {hypothesis.SignalId}: {hypothesis.Hypothesis}",
            serializerOptions: serializerOptions,
            cancellationToken: cancellationToken);

        await context.SendMessageAsync(response.Result, cancellationToken: cancellationToken);
    }

    public ValueTask ResetAsync() => default;
}

/// <summary>Policy gate: approves safe plans, rejects destructive ones, and emits a typed verdict.</summary>
internal sealed class ReviewBridgeExecutor() : Executor("review"), IResettableExecutor
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        protocolBuilder.RouteBuilder.AddHandler<SolutionPlan>(this.ReviewAsync);
        return protocolBuilder.SendsMessage<ConfidenceVerdict>();
    }

    private ValueTask ReviewAsync(SolutionPlan plan, IWorkflowContext context, CancellationToken cancellationToken)
    {
        bool clearToExecute = !plan.Approach.Contains("destructive", StringComparison.OrdinalIgnoreCase);
        var verdict = new ConfidenceVerdict(
            plan.SignalId,
            clearToExecute,
            clearToExecute ? "Policy verification succeeded." : "Destructive approach requires manual review.");

        return context.SendMessageAsync(verdict, cancellationToken: cancellationToken);
    }

    public ValueTask ResetAsync() => default;
}

/// <summary>Terminal sink: yields a final string for either a verdict (auto path) or a low-severity signal (info path).</summary>
internal sealed class OutputSinkExecutor(string id, string prefix) : Executor(id), IResettableExecutor
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        protocolBuilder.RouteBuilder
            .AddHandler<ConfidenceVerdict>((verdict, context, cancellationToken) =>
                context.YieldOutputAsync($"{prefix}: {verdict.SignalId} — {verdict.Reason}", cancellationToken))
            .AddHandler<IncidentSignal>((signal, context, cancellationToken) =>
                context.YieldOutputAsync($"{prefix}: {signal.Id} — below the auto-remediation threshold, logged.", cancellationToken));

        return protocolBuilder.YieldsOutput<string>();
    }

    public ValueTask ResetAsync() => default;
}
