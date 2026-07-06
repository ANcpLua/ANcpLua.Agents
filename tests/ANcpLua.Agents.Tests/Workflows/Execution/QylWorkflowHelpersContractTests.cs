using System.Reflection;
using ANcpLua.Agents.Workflows;

namespace ANcpLua.Agents.Tests.Workflows.Execution;

public sealed class QylWorkflowHelpersContractTests
{
    [Fact]
    public void QylWorkflowExecutionExtensions_Publishes_Stable_Workflow_Helpers()
    {
        var workflowExtensions = typeof(QylWorkflowExecutionExtensions);
        var agentExtensions = typeof(QylAgentWorkflowExtensions);

        AssertPublicMethodByName(workflowExtensions, "RunQylAsync", 4);
        AssertPublicMethodByName(workflowExtensions, "StreamQylAsync", 4);
        AssertPublicMethodByName(workflowExtensions, "StreamQylCheckpointedAsync", 5);
        AssertPublicMethodByName(workflowExtensions, "ResumeQylAsync", 4);
        AssertPublicMethodByName(workflowExtensions, "StreamQylAgentsAsync", 5);
        AssertPublicMethodByName(workflowExtensions, "WithQylTelemetry", 3);

        AssertPublicMethodByName(agentExtensions, "BuildQylSequential", 3);
        AssertPublicMethodByName(agentExtensions, "BuildQylSequential", 2);
        AssertPublicMethodByName(agentExtensions, "BuildQylConcurrent", 1);
        AssertPublicMethodByName(agentExtensions, "BuildQylGroupChat", 2);
        AssertPublicMethodByName(agentExtensions, "AsQylSequentialAgent", 3);
        AssertPublicMethodByName(agentExtensions, "AsQylConcurrentAgent", 2);
        AssertPublicMethodByName(agentExtensions, "StreamQylSequentialAsync", 5);
        AssertPublicMethodByName(agentExtensions, "StreamQylConcurrentAsync", 5);
    }

    [Fact]
    public void QylWorkflowExecutionHelpers_Exist_AsInternal_Stable_Implementation_Support()
    {
        var resolvedHelperType = typeof(QylWorkflowExecutionExtensions).Assembly
            .GetType("ANcpLua.Agents.Workflows.Execution.QylWorkflowExecutionHelpers");

        resolvedHelperType.Should().NotBeNull();
        var helperType = resolvedHelperType;
        helperType.IsNotPublic.Should().BeTrue();
        helperType
            .GetMethod(
                "StreamQylAgentsAsync",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Should().NotBeNull();
    }

    private static void AssertPublicMethodByName(Type type, string methodName, int parameterCount)
    {
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Should()
            .Contain(method => method.Name == methodName && method.GetParameters().Length == parameterCount);
    }

}
