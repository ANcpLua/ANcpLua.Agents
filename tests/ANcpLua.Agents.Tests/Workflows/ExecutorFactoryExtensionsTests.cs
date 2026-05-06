using System.Reflection;
using ANcpLua.Agents.Workflows;

namespace ANcpLua.Agents.Tests.Workflows;

public sealed class ExecutorFactoryExtensionsTests
{
    [Fact]
    public void QylExecutorFactoryExtensions_Contains_MaintainerGrade_Helper_Methods()
    {
        var methods = typeof(QylExecutorFactoryExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        methods.Should().Contain(static method => method.Name == "QylFunction" && method.IsGenericMethodDefinition);
        methods.Should().Contain(static method => method.Name == "QylFunctionAsync" && method.IsGenericMethodDefinition);
        methods.Should().Contain(static method => method.Name == "QylCollect" && method.IsGenericMethodDefinition);
        methods.Should().Contain(static method => method.Name == "QylSum" && !method.IsGenericMethodDefinition);
        methods.Should().Contain(static method => method.Name == "QylAgentExecutor" && method.IsGenericMethodDefinition);
    }

    [Fact]
    public void QylExecutorFactoryExtensions_DoesNot_Migrate_GeneratorExecutorExample()
    {
        typeof(QylExecutorFactoryExtensions).Assembly
            .GetType("ANcpLua.Agents.Workflows.QylGeneratorExecutorExample")
            .Should().BeNull();
    }
}
