using System.Reflection;
using ANcpLua.Agents.Foundry;
using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Agents.Tests.Hosting.Foundry;

public sealed class FoundryAgentExtensionsTests
{
    [Fact]
    public void QylFoundryAgentExtensions_Exposes_Agent_Facade_Surface()
    {
        var type = typeof(QylFoundryAgentExtensions);

        AssertPublicMethodByName(type, "AsQylAIAgent", 9);
        AssertPublicMethodByName(type, "AsQylAIAgent", 5);
        AssertPublicMethodByName(type, "CreateQylAzureAgentProvider", 2);
        AssertPublicMethodByName(type, "CreateQylHostedMcpToolbox", 2);
        AssertPublicMethodByName(type, "BuildQylFoundryEvals", 3);
    }

    [Fact]
    public void QylFoundryEvalExtensions_Exposes_Evaluation_Helpers()
    {
        var type = typeof(QylFoundryEvalExtensions);

        AssertPublicMethodByName(type, "EvaluateQylTracesAsync", 11);
        AssertPublicMethodByName(type, "EvaluateQylFoundryTargetAsync", 9);
    }

    [Fact]
    public void QylFoundryMemoryExtensions_Exposes_Memory_Provider_Factories()
    {
        var methods = typeof(QylFoundryMemoryExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static method => method.Name == "AsQylFoundryMemoryProviderAsync")
            .ToArray();

        methods.Should().HaveCount(2);
        methods.Should().ContainSingle(static method =>
            method.GetParameters()[2].ParameterType == typeof(string));
        methods.Should().ContainSingle(static method =>
            method.GetParameters()[2].ParameterType.Name.ContainsOrdinal("Func"));
    }

    [Fact]
    public void QylFoundryDeclarativeWorkflowExtensions_Exposes_Options_Builder()
    {
        var type = typeof(QylFoundryDeclarativeWorkflowExtensions);

        AssertPublicMethodByName(type, "BuildQylFoundryDeclarativeWorkflowOptions", 5);
    }

    private static void AssertPublicMethodByName(Type type, string methodName, int parameterCount) =>
        type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Should()
            .ContainSingle(method => method.Name == methodName && method.GetParameters().Length == parameterCount);
}
