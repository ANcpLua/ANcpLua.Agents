using System.Reflection;
using ANcpLua.Agents.Hosting.Azure;

namespace ANcpLua.Agents.Tests.Hosting.Azure;

public sealed class AzureFunctionsHostingExtensionsTests
{
    [Fact]
    public void QylAzureFunctionsHostingExtensions_Exposes_Durable_Facade_Surface()
    {
        var type = typeof(QylAzureFunctionsHostingExtensions);

        AssertPublicMethodByName(type, "ConfigureQylDurableAgents", 2);
        AssertPublicMethodByName(type, "ConfigureQylDurableWorkflows", 2);
        AssertPublicMethodByName(type, "ConfigureQylDurableOptions", 2);
        AssertPublicMethodByName(type, "AddQylAIAgent", 3);
        AssertPublicMethodByName(type, "AddQylAIAgent", 4);
        AssertPublicMethodByName(type, "AddQylAIAgentFactory", 4);
        AssertPublicMethodByName(type, "AddQylAIAgentFactory", 5);
        AssertPublicMethodByName(type, "AddQylDurable", 4);
        AssertPublicMethodByName(type, "AddQylDurableAgents", 4);
        AssertPublicMethodByName(type, "AddQylDurableWorkflows", 4);
        AssertPublicMethodByName(type, "AddQylWorkflow", 3);
        AssertPublicMethodByName(type, "AddQylWorkflow", 4);
        AssertPublicMethodByName(type, "AsQylDurableAgentProxy", 3);
    }

    private static void AssertPublicMethodByName(Type type, string methodName, int parameterCount)
    {
        type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Should()
            .Contain(method => method.Name == methodName && method.GetParameters().Length == parameterCount);
    }
}
