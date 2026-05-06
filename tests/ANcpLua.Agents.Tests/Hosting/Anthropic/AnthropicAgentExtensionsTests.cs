using System.Reflection;
using ANcpLua.Agents.Hosting.Anthropic;

namespace ANcpLua.Agents.Tests.Hosting.Anthropic;

public sealed class AnthropicAgentExtensionsTests
{
    [Fact]
    public void QylAnthropicAgentExtensions_Exposes_Source_Compatible_Client_Facades()
    {
        var methods = typeof(QylAnthropicAgentExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static method => method.Name == nameof(QylAnthropicAgentExtensions.AsQylAnthropicAgent))
            .ToArray();

        methods.Should().HaveCount(2);
        methods.Should().ContainSingle(static method =>
            method.GetParameters()[0].ParameterType.FullName == "Anthropic.IAnthropicClient" &&
            method.GetParameters().Length == 10);
        methods.Should().ContainSingle(static method =>
            method.GetParameters()[0].ParameterType.FullName == "Anthropic.IAnthropicClient" &&
            IsHostedMcpServerToolList(method.GetParameters()[2].ParameterType));
    }

    private static bool IsHostedMcpServerToolList(Type parameterType)
    {
        return parameterType.IsGenericType &&
               parameterType.GetGenericTypeDefinition() == typeof(IList<>) &&
               parameterType.GetGenericArguments()[0].FullName == "Microsoft.Extensions.AI.HostedMcpServerTool";
    }
}
