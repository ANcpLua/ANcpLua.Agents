using System.Reflection;
using ANcpLua.Agents.Hosting.Foundry;

namespace ANcpLua.Agents.Tests.Hosting.Foundry;

public sealed class FoundryHostingExtensionsTests
{
    [Fact]
    public void QylFoundryHostingExtensions_Exposes_Preview_Facade_Surface()
    {
        var type = typeof(QylFoundryHostingExtensions);

        AssertPublicMethodByName(type, "AddQylFoundryResponses", 1);
        AssertPublicMethodByName(type, "AddQylFoundryResponses", 3);
        AssertPublicMethodByName(type, "AddQylFoundryToolboxes", 2);
        AssertPublicMethodByName(type, "AddQylFoundryToolboxes", 3);
        AssertPublicMethodByName(type, "MapQylFoundryResponses", 2);
        AssertPublicMethodByName(type, "GetQylToolboxToolsAsync", 4);
    }

    private static void AssertPublicMethodByName(Type type, string methodName, int parameterCount)
    {
        type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Should()
            .ContainSingle(method => method.Name == methodName && method.GetParameters().Length == parameterCount);
    }

}
