// Showcase: human-in-the-loop + checkpoint/resume workflow, fully offline.
//
// Combination:
//   MAF Microsoft.Agents.AI.Workflows (RequestPort / external-call / CheckpointManager / RestoreCheckpoint)
//   x ANcpLua.Agents.Workflows.QylWorkflowBuilderExtensions.AddQylHumanInTheLoop<TRequest,TResponse>
//   x ANcpLua.Agents.Workflows.QylCheckpointStoreExtensions.AddQylInMemoryCheckpointing
//   x ANcpLua.Agents.Instrumentation (QylAgentFactory) over ANcpLua.Agents.Testing FakeChatClient (no API keys).
//
// Scenario: an expense-approval workflow pauses for an external (human) approval decision.
//   1. A FakeChatClient-backed agent drafts the approval rationale (offline).
//   2. The ApprovalGate executor emits an ApprovalRequest through a human-in-the-loop port
//      added via AddQylHumanInTheLoop; the workflow halts and surfaces a RequestInfoEvent
//      (this is the "BEFORE" state — no answer supplied yet).
//   3. Checkpoints are captured at each super step using a CheckpointManager resolved from
//      the AddQylInMemoryCheckpointing DI helper.
//   4. We resume from the captured checkpoint, supply the human ApprovalDecision, and the
//      gate yields the final outcome (the "AFTER" state).

using System.ComponentModel;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

const string ApprovalPortId = "ExpenseApproval";

// --- Offline agent: drafts the approval rationale via a seeded FakeChatClient. ---
using var chatClient = new FakeChatClient();
chatClient.WithResponse(
    "Expense within policy: travel category, under the $1,000 manager threshold. Recommend approval.");

AIAgent reviewer = QylAgentFactory.Create(chatClient, options => options
    .WithName("expense-reviewer")
    .WithDescription("Drafts a short rationale for an expense-approval decision.")
    .WithInstructions("You summarize whether an expense looks reasonable and within policy."));

var claim = new ExpenseClaim("Conference travel", 820m, "alice@contoso.com");

AgentSession reviewerSession = await reviewer.CreateSessionAsync();
AgentResponse draft = await reviewer.RunAsync(
    $"Expense '{claim.Purpose}' for {claim.Amount:C} submitted by {claim.Submitter}.",
    reviewerSession);

Console.WriteLine("=== Reviewer draft (offline agent) ===");
Console.WriteLine(draft.Text);
Console.WriteLine();

// --- Build the human-in-the-loop workflow via the Qyl facade. ---
// AddQylHumanInTheLoop wires gate -> port (carries ApprovalRequest) and port -> gate
// (carries ApprovalDecision), turning the gate into a pausing external-call source.
var gate = new ApprovalGate(claim, draft.Text);

Workflow workflow = new WorkflowBuilder(gate)
    .AddQylHumanInTheLoop<ApprovalRequest, ApprovalDecision>(gate, ApprovalPortId)
    .WithOutputFrom(gate)
    .Build();

// --- Checkpoint manager resolved through the Qyl DI helper. ---
await using ServiceProvider provider = new ServiceCollection()
    .AddQylInMemoryCheckpointing()
    .BuildServiceProvider();
var checkpointManager = provider.GetRequiredService<CheckpointManager>();

// --- Phase 1: run until the workflow halts waiting for the human (BEFORE). ---
Console.WriteLine("=== BEFORE: running until human approval is required ===");
List<CheckpointInfo> checkpoints = [];
ExternalRequest? pendingRequest = null;

await using (StreamingRun firstRun =
             await InProcessExecution.RunStreamingAsync(workflow, claim, checkpointManager))
{
    await foreach (WorkflowEvent evt in firstRun.WatchStreamAsync())
    {
        switch (evt)
        {
            case RequestInfoEvent requestEvt when requestEvt.Request.PortInfo.PortId == ApprovalPortId:
                pendingRequest = requestEvt.Request;
                if (requestEvt.Request.TryGetDataAs(out ApprovalRequest? ask))
                {
                    Console.WriteLine($"PAUSED for human approval on port '{requestEvt.Request.PortInfo.PortId}':");
                    Console.WriteLine($"  Purpose : {ask.Purpose}");
                    Console.WriteLine($"  Amount  : {ask.Amount:C}");
                    Console.WriteLine($"  Rationale: {ask.Rationale}");
                }

                break;

            case SuperStepCompletedEvent stepEvt when stepEvt.CompletionInfo?.Checkpoint is { } checkpoint:
                checkpoints.Add(checkpoint);
                Console.WriteLine($"  [checkpoint captured at super step {checkpoints.Count}]");
                break;
        }

        // Stop once the workflow has halted with a pending external request.
        if (pendingRequest is not null)
        {
            break;
        }
    }
}

if (pendingRequest is null || checkpoints.Count == 0)
{
    throw new InvalidOperationException("Workflow did not pause for human input as expected.");
}

Console.WriteLine($"No answer supplied yet. {checkpoints.Count} checkpoint(s) available for resume.");
Console.WriteLine();

// --- Phase 2: resume from the captured checkpoint and supply the human answer (AFTER). ---
Console.WriteLine("=== AFTER: resuming from checkpoint and supplying the human decision ===");
CheckpointInfo resumeFrom = checkpoints[^1];
var decision = new ApprovalDecision(true, "Approved by manager Bob.");
string? outcome = null;

await using (StreamingRun resumedRun =
             await InProcessExecution.ResumeStreamingAsync(workflow, resumeFrom, checkpointManager))
{
    await foreach (WorkflowEvent evt in resumedRun.WatchStreamAsync())
    {
        switch (evt)
        {
            case RequestInfoEvent requestEvt when requestEvt.Request.PortInfo.PortId == ApprovalPortId:
                Console.WriteLine($"  Supplying human decision: approved={decision.Approved} ({decision.Note})");
                await resumedRun.SendResponseAsync(requestEvt.Request.CreateResponse(decision));
                break;

            case WorkflowOutputEvent outputEvt when outputEvt.As<string>() is { } final:
                outcome = final;
                break;
        }

        if (outcome is not null)
        {
            break;
        }
    }
}

Console.WriteLine();
Console.WriteLine("=== Final outcome ===");
Console.WriteLine(outcome ?? "(workflow produced no output)");

// ---------------------------------------------------------------------------
// Workflow message contracts.
// ---------------------------------------------------------------------------

/// <summary>The expense being submitted (the workflow seed input).</summary>
internal sealed record ExpenseClaim(string Purpose, decimal Amount, string Submitter);

/// <summary>The request handed to the human-in-the-loop port (TRequest).</summary>
internal sealed record ApprovalRequest(string Purpose, decimal Amount, string Rationale);

/// <summary>The decision the human supplies back through the port (TResponse).</summary>
internal sealed record ApprovalDecision(bool Approved, string Note);

/// <summary>
/// Source executor for the human-in-the-loop port. It handles the seed <see cref="ExpenseClaim"/>
/// by emitting an <see cref="ApprovalRequest"/> (which the port surfaces as a RequestInfoEvent),
/// and handles the returning <see cref="ApprovalDecision"/> by yielding the workflow output.
/// </summary>
internal sealed class ApprovalGate(ExpenseClaim claim, string rationale) : Executor("ApprovalGate")
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        protocolBuilder.RouteBuilder
            .AddHandler<ExpenseClaim>(this.RequestApprovalAsync)
            .AddHandler<ApprovalDecision>(this.CompleteAsync);

        return protocolBuilder
            .SendsMessage<ApprovalRequest>()
            .YieldsOutput<string>();
    }

    private ValueTask RequestApprovalAsync(ExpenseClaim message, IWorkflowContext context, CancellationToken cancellationToken)
        => context.SendMessageAsync(
            new ApprovalRequest(message.Purpose, message.Amount, rationale),
            cancellationToken: cancellationToken);

    private ValueTask CompleteAsync(ApprovalDecision decision, IWorkflowContext context, CancellationToken cancellationToken)
    {
        var verdict = decision.Approved ? "APPROVED" : "REJECTED";
        return context.YieldOutputAsync(
            $"Expense '{claim.Purpose}' for {claim.Amount:C} by {claim.Submitter}: {verdict}. {decision.Note}",
            cancellationToken);
    }
}
