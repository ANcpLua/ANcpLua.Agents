using ANcpLua.Agents.Facades;
using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace AgentTesting.Harness;

// (f) Folded from the former AgentWorkflow.Chain sample. A normalize -> triage(agent) -> format chain
//     assembled with AddQylChain and run offline via RunQylAsync. The standalone sample only printed
//     the result; here the assertion proves the agent stage's classification flows through to the
//     final report — it goes red if the chain order breaks or the agent stage is skipped.
public sealed class ChainWorkflowTests
{
    [Fact]
    public async Task Chain_NormalizeTriageFormat_FlowsAgentOutputToReport()
    {
        using var chatClient = new FakeChatClient();
        chatClient.WithResponse("priority=HIGH; team=billing; reason=payment failure reported");

        AIAgent triageAgent = new QylAgentOptionsBuilder()
            .WithName("triage-agent")
            .WithInstructions("Classify the support ticket as priority=...; team=...; reason=...")
            .BuildAgent(chatClient);

        FunctionExecutor<string, string> normalize =
            QylExecutorFactoryExtensions.QylFunction<string, string>(
                "normalize", static raw => raw.Trim().ReplaceLineEndings(" "));
        FunctionExecutor<string, string> triage =
            QylExecutorFactoryExtensions.QylAgentExecutor<string>(
                "triage", triageAgent, static normalized => $"Triage this ticket: {normalized}");
        FunctionExecutor<string, string> format =
            QylExecutorFactoryExtensions.QylFunction<string, string>(
                "format", static classification => $"[TICKET REPORT] {classification}");

        ExecutorBinding source = normalize;
        ExecutorBinding triageStage = triage;
        ExecutorBinding formatStage = format;

        Workflow workflow = new WorkflowBuilder(source)
            .AddQylChain(source, [triageStage, formatStage])
            .WithOutputFrom(formatStage)
            .Build();

        Run run = await workflow.RunQylAsync("  Customer: my payment failed twice today!  ");

        string? output = run.OutgoingEvents
            .OfType<WorkflowOutputEvent>()
            .Select(static evt => evt.As<string>())
            .LastOrDefault(static text => text is not null);

        output.Should().NotBeNull();
        output.Should().StartWith("[TICKET REPORT]");
        output.Should().Contain("priority=HIGH");
    }
}
