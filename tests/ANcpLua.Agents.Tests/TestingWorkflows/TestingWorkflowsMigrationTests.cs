using ANcpLua.Agents.Testing.Workflows;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Agents.Tests.TestingWorkflows;

public sealed class TestingWorkflowsMigrationTests
{
    [Fact]
    public void TestPorts_Create_ReturnsNamedPorts()
    {
        var ports = TestPorts.Create<string, string>("ask-user", "approve");

        ports.Keys.Should().BeEquivalentTo(["ask-user", "approve"]);
        ports.Values.Should().AllBeAssignableTo<RequestPort>();
    }

    [Fact]
    public void WorkflowRunHarness_For_ReturnsBuilder()
    {
        var builder = WorkflowRunHarness.For(CreateWorkflow);

        builder.Should().BeOfType<WorkflowRunHarnessBuilder>();
    }

    private static Workflow CreateWorkflow()
        => new WorkflowBuilder(new EchoExecutor()).Build();

    private sealed class EchoExecutor()
        : Executor<string, string>(nameof(EchoExecutor))
    {
        public override ValueTask<string> HandleAsync(
            string message,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
            => new(message);
    }
}
