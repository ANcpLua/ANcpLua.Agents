using ANcpLua.Agents.Facades;
using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace AgentTesting.Harness;

// (g) Folded from the former AgentWorkflow.Switch sample. AddQylSwitch routes on an offline agent's
//     one-word label. The standalone sample printed which branch ran; here the routing IS the
//     assertion — URGENT must reach UrgentBranch, STANDARD must fall through to the default branch.
//     A broken predicate or default wiring flips the expected ExecutorId and the test goes red.
public sealed class SwitchWorkflowTests
{
    [Theory]
    [InlineData("URGENT", "UrgentBranch", "paged")]
    [InlineData("STANDARD", "StandardBranch", "backlog")]
    public async Task Switch_RoutesByAgentLabel(string agentLabel, string expectedBranch, string expectedText)
    {
        using var classifierClient = new FakeChatClient();
        classifierClient.WithResponse(agentLabel);

        ChatClientAgent classifier = new QylAgentOptionsBuilder()
            .WithName("ticket-classifier")
            .WithInstructions("Reply with exactly one word: URGENT or STANDARD.")
            .BuildAgent(classifierClient);

        var triage = new TriageExecutor(classifier);
        var urgent = new UrgentBranch();
        var standard = new StandardBranch();

        Workflow workflow = new WorkflowBuilder(triage)
            .AddQylSwitch(triage, switchBuilder => switchBuilder
                .AddCase<string>(label => label == "URGENT", urgent)
                .WithDefault(standard))
            .WithOutputFrom(urgent, standard)
            .Build();

        await using Run run = await InProcessExecution.RunAsync(workflow, new Ticket("T-1", "ticket body"));

        WorkflowOutputEvent output = run.NewEvents.OfType<WorkflowOutputEvent>().Single();
        output.ExecutorId.Should().Be(expectedBranch);
        output.Data!.ToString().Should().Contain(expectedText);
    }

    private sealed record Ticket(string Id, string Body);

    private sealed class TriageExecutor(AIAgent classifier) : Executor<Ticket, string>("TriageExecutor")
    {
        public override async ValueTask<string> HandleAsync(
            Ticket message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            AgentResponse response = await classifier.RunAsync(message.Body, cancellationToken: cancellationToken);
            return response.Text.Trim();
        }
    }

    [YieldsOutput(typeof(string))]
    private sealed class UrgentBranch() : Executor<string>("UrgentBranch")
    {
        public override ValueTask HandleAsync(
            string label, IWorkflowContext context, CancellationToken cancellationToken = default)
            => context.YieldOutputAsync($"paged on-call engineer ({label})", cancellationToken);
    }

    [YieldsOutput(typeof(string))]
    private sealed class StandardBranch() : Executor<string>("StandardBranch")
    {
        public override ValueTask HandleAsync(
            string label, IWorkflowContext context, CancellationToken cancellationToken = default)
            => context.YieldOutputAsync($"queued for the backlog ({label})", cancellationToken);
    }
}
