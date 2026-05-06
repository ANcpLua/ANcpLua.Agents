using System.Reflection;
using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Tests.Workflows;

public sealed class WorkflowStableHelpersTests
{
    [Fact]
    public void QylWorkflowBuilderExtensions_Publishes_MaintainerGrade_Workflow_Builder_Helpers()
    {
        var builderExtensions = typeof(QylWorkflowBuilderExtensions);

        AssertPublicMethodByName(builderExtensions, "AddQylChain", 4);
        AssertPublicMethodByName(builderExtensions, "AddQylSwitch", 3);
        AssertPublicMethodByName(builderExtensions, "AddQylHumanInTheLoop", 3);
        AssertPublicMethodByName(builderExtensions, "ForwardQyl", 4);
        AssertPublicMethodByName(builderExtensions, "ForwardQylExcept", 3);

        var forwardMethods = builderExtensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static m => m.Name == "ForwardQyl");

        forwardMethods.Should().HaveCount(3);
        forwardMethods.Count(static m => m.GetParameters().Length == 3).Should().Be(2);
        forwardMethods.Count(static m => m.GetParameters().Length == 4).Should().Be(1);
    }

    [Fact]
    public void QylWorkflowFactoryExtensions_Publishes_MaintainerGrade_Workflow_Factory_Helpers()
    {
        var factoryExtensions = typeof(QylWorkflowFactoryExtensions);
        var factoryInterface = typeof(IQylWorkflowFactory);

        factoryInterface.IsInterface.Should().BeTrue();

        factoryInterface.GetMethod(nameof(IQylWorkflowFactory.Build)).Should().NotBeNull();
        AssertPublicMethodByName(factoryExtensions, "AddQylWorkflow", 2);
        AssertPublicMethodByName(factoryExtensions, "GetQylWorkflow", 2);
    }

    [Fact]
    public void AddQylWorkflow_Binds_Keyed_Workflow()
    {
        var services = new ServiceCollection();
        services.AddQylWorkflow<TestWorkflowFactory>("unit-test-workflow");
        using var provider = services.BuildServiceProvider();

        var workflow = provider.GetQylWorkflow("unit-test-workflow");
        var keyedWorkflow = provider.GetRequiredKeyedService<Workflow>("unit-test-workflow");

        workflow.Should().NotBeNull();
        keyedWorkflow.Should().NotBeNull();
        workflow.Should().BeSameAs(keyedWorkflow);
    }

    private static void AssertPublicMethodByName(Type type, string methodName, int parameterCount)
    {
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Should()
            .Contain(method => method.Name == methodName && method.GetParameters().Length == parameterCount);
    }

    private sealed class TestWorkflowFactory : IQylWorkflowFactory
    {
        public Workflow Build()
        {
            return BuildDemoWorkflow();
        }

        private static Workflow BuildDemoWorkflow()
        {
            var uppercase = new UppercaseExecutor();
            var sink = new UppercaseSinkExecutor();

            return new WorkflowBuilder(uppercase)
                .AddEdge(uppercase, sink)
                .WithOutputFrom(sink)
                .Build();
        }

        private sealed class UppercaseExecutor() : Executor<string, string>(nameof(UppercaseExecutor), declareCrossRunShareable: true)
        {
            public override ValueTask<string> HandleAsync(string message, IWorkflowContext context,
                CancellationToken cancellationToken = default)
                => new(message.ToUpperInvariant());
        }

        private sealed class UppercaseSinkExecutor()
            : Executor<string, string>(nameof(UppercaseSinkExecutor), declareCrossRunShareable: true)
        {
            public override ValueTask<string> HandleAsync(string message, IWorkflowContext context,
                CancellationToken cancellationToken = default)
                => new(message);
        }
    }
}
