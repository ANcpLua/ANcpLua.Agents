using System.Reflection;
using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Agents.Tests.Workflows;

public sealed class ExecutorFactoryExtensionsTests
{
    [Fact]
    public void QylExecutorFactoryExtensions_Contains_MaintainerGrade_Helper_Methods()
    {
        var methods = typeof(QylExecutorFactoryExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        methods.Should().ContainSingle(static method =>
            method.Name == "QylFunction" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 2 &&
            IsGenericReturn(method, typeof(FunctionExecutor<,>)) &&
            ParametersMatch(method, typeof(string), typeof(Func<,>)));
        methods.Should().ContainSingle(static method =>
            method.Name == "QylFunctionAsync" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 2 &&
            IsGenericReturn(method, typeof(FunctionExecutor<,>)) &&
            ParametersMatch(method, typeof(string), typeof(Func<,,,>)));
        methods.Should().ContainSingle(static method =>
            method.Name == "QylCollect" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            IsGenericReturn(method, typeof(AggregatingExecutor<,>)) &&
            ParametersMatch(method, typeof(string)));
        methods.Should().ContainSingle(static method =>
            method.Name == "QylSum" &&
            !method.IsGenericMethodDefinition &&
            IsGenericReturn(method, typeof(AggregatingExecutor<,>)) &&
            ParametersMatch(method, typeof(string)));
        methods.Should().ContainSingle(static method =>
            method.Name == "QylAgentExecutor" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 2 &&
            IsGenericReturn(method, typeof(FunctionExecutor<,>)) &&
            ParametersMatch(method, typeof(string), typeof(AIAgent), typeof(Func<,>)));
        methods.Should().ContainSingle(static method =>
            method.Name == "QylAgentExecutor" &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1 &&
            IsGenericReturn(method, typeof(FunctionExecutor<,>)) &&
            ParametersMatch(method, typeof(string), typeof(AIAgent), typeof(Func<,>)));
    }

    [Fact]
    public void QylExecutorFactoryExtensions_DoesNot_Migrate_GeneratorExecutorExample()
    {
        typeof(QylExecutorFactoryExtensions).Assembly
            .GetType("ANcpLua.Agents.Workflows.QylGeneratorExecutorExample")
            .Should().BeNull();
    }

    private static bool IsGenericReturn(MethodInfo method, Type genericTypeDefinition)
    {
        return method.ReturnType.IsGenericType &&
               method.ReturnType.GetGenericTypeDefinition() == genericTypeDefinition;
    }

    private static bool ParametersMatch(MethodInfo method, params Type[] expectedTypes)
    {
        var actual = method.GetParameters().Select(static parameter => parameter.ParameterType).ToArray();
        if (actual.Length != expectedTypes.Length)
        {
            return false;
        }

        return actual.Zip(expectedTypes).All(static pair =>
        {
            var (actualType, expectedType) = pair;
            return expectedType.IsGenericTypeDefinition
                ? actualType.IsGenericType && actualType.GetGenericTypeDefinition() == expectedType
                : actualType == expectedType;
        });
    }
}
