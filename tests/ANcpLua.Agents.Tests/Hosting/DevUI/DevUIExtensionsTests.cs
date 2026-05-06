using System.Reflection;
using ANcpLua.Agents.Hosting.DevUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ANcpLua.Agents.Tests.Hosting.DevUI;

public sealed class DevUIExtensionsTests
{
    [Fact]
    public void QylDevUIExtensions_WhenInspected_ExposesExpectedFacadeSignatures()
    {
        var type = typeof(QylDevUIExtensions);

        AssertPublicMethod(type, "AddQylDevUI", typeof(IHostApplicationBuilder), typeof(IHostApplicationBuilder));
        AssertPublicMethod(type, "AddQylDevUI", typeof(IServiceCollection), typeof(IServiceCollection));
        AssertPublicMethod(type, "MapQylDevUI", typeof(IEndpointConventionBuilder), typeof(IEndpointRouteBuilder));
    }

    private static void AssertPublicMethod(Type type, string methodName, Type returnType, params Type[] parameterTypes)
    {
        type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Should()
            .ContainSingle(method =>
                method.Name == methodName &&
                method.ReturnType == returnType &&
                method.GetParameters().Select(static parameter => parameter.ParameterType).SequenceEqual(parameterTypes));
    }
}
