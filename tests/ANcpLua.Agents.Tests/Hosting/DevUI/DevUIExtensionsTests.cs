using System.Reflection;
using ANcpLua.Agents.Hosting.DevUI;

namespace ANcpLua.Agents.Tests.Hosting.DevUI;

public sealed class DevUIExtensionsTests
{
    [Fact]
    public void QylDevUIExtensions_Exposes_Registration_And_Map_Facades()
    {
        var type = typeof(QylDevUIExtensions);

        AssertPublicMethodByName(type, "AddQylDevUI", 1);
        AssertPublicMethodByName(type, "MapQylDevUI", 1);
    }

    private static void AssertPublicMethodByName(Type type, string methodName, int parameterCount)
    {
        type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Should()
            .Contain(method => method.Name == methodName && method.GetParameters().Length == parameterCount);
    }
}
