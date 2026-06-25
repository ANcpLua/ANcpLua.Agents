// Showcase: a conditional-routing workflow whose branch is chosen by an offline agent.
//
// Combination:
//   MAF Microsoft.Agents.AI.Workflows (Executor / WorkflowBuilder / InProcessExecution /
//     WorkflowOutputEvent) and the ChatClientAgent that classifies each ticket
//   x ANcpLua.Agents.Workflows (QylWorkflowBuilderExtensions.AddQylSwitch + SwitchBuilder)
//   x ANcpLua.Agents (QylAgentOptionsBuilder) over ANcpLua.Agents.Testing FakeChatClient.
//
// A TriageExecutor asks a ChatClientAgent (backed by an offline FakeChatClient) to label a
// support ticket "URGENT" or "STANDARD". AddQylSwitch then routes that label to one of two
// terminal branches. We run the same workflow shape twice -- once with a fake seeded to say
// "URGENT", once seeded to say "STANDARD" -- and print which branch executor handled each.

using ANcpLua.Agents.Facades;
using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

// Two tickets. The fake stands in for "what the classifier agent says about THIS ticket",
// so each run gets a freshly seeded client -> routing follows content, not seeding order.
var inbox = new (Ticket Ticket, string AgentLabel)[]
{
    (new Ticket("T-1001", "Production is down, customers cannot check out!"), "URGENT"),
    (new Ticket("T-1002", "Please update the font on the about page when you get a chance."), "STANDARD")
};

foreach (var (ticket, agentLabel) in inbox)
{
    // Fresh, offline classifier agent for this ticket. No network, no API keys.
    using var classifierClient = new FakeChatClient();
    classifierClient.WithResponse(agentLabel);

    ChatClientAgent classifier = new QylAgentOptionsBuilder()
        .WithName("ticket-classifier")
        .WithDescription("Labels a support ticket as URGENT or STANDARD.")
        .WithInstructions("Reply with exactly one word: URGENT or STANDARD.")
        .BuildAgent(classifierClient);

    Workflow workflow = BuildWorkflow(classifier);

    await using Run run = await InProcessExecution.RunAsync(workflow, ticket);

    foreach (WorkflowEvent evt in run.NewEvents)
    {
        if (evt is WorkflowOutputEvent output)
        {
            Console.WriteLine($"{ticket.Id} -> branch '{output.ExecutorId}': {output.Data}");
        }
        else if (evt is ExecutorFailedEvent failed)
        {
            Console.Error.WriteLine($"{ticket.Id} -> executor '{failed.ExecutorId}' failed: {failed.Data}");
        }
    }
}

// Builds: TriageExecutor --(AddQylSwitch on the agent label)--> { UrgentBranch | StandardBranch }.
static Workflow BuildWorkflow(AIAgent classifier)
{
    var triage = new TriageExecutor(classifier);
    var urgent = new UrgentBranch();
    var standard = new StandardBranch();

    return new WorkflowBuilder(triage)
        .AddQylSwitch(triage, switchBuilder => switchBuilder
            .AddCase<string>(label => label == "URGENT", urgent)
            .WithDefault(standard))
        .WithOutputFrom(urgent, standard)
        .Build();
}

/// <summary>A support ticket flowing into the workflow.</summary>
internal sealed record Ticket(string Id, string Body);

/// <summary>
/// Source executor: classifies the ticket with the offline agent and emits the label that the
/// switch routes on.
/// </summary>
internal sealed class TriageExecutor(AIAgent classifier)
    : Executor<Ticket, string>("TriageExecutor")
{
    public override async ValueTask<string> HandleAsync(
        Ticket message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        AgentResponse response = await classifier.RunAsync(message.Body, cancellationToken: cancellationToken);
        return response.Text.Trim();
    }
}

/// <summary>Terminal branch for the matched (URGENT) case.</summary>
[YieldsOutput(typeof(string))]
internal sealed class UrgentBranch() : Executor<string>("UrgentBranch")
{
    public override ValueTask HandleAsync(
        string label,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
        => context.YieldOutputAsync($"paged on-call engineer ({label})", cancellationToken);
}

/// <summary>Terminal branch for the default (STANDARD) case.</summary>
[YieldsOutput(typeof(string))]
internal sealed class StandardBranch() : Executor<string>("StandardBranch")
{
    public override ValueTask HandleAsync(
        string label,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
        => context.YieldOutputAsync($"queued for the backlog ({label})", cancellationToken);
}
