using System.Reflection;
using System.ClientModel;
using ANcpLua.Agents.Hosting.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using OpenAI.Responses;

namespace ANcpLua.Agents.Tests.Hosting.OpenAI;

public sealed class OpenAIClientExtensionsTests
{
    [Fact]
    public void QylOpenAIClientExtensions_Exposes_Client_To_Agent_Facades()
    {
        var methods = typeof(QylOpenAIClientExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static method => method.Name == nameof(QylOpenAIClientExtensions.AsQylOpenAIAgent))
            .ToArray();

        methods.Should().HaveCount(5);
        methods.Should().ContainSingle(static method =>
            method.ReturnType == typeof(ChatClientAgent) &&
            method.GetParameters()[0].ParameterType.FullName == "OpenAI.Chat.ChatClient" &&
            method.GetParameters().Length == 8);
        methods.Should().ContainSingle(static method =>
            method.ReturnType == typeof(ChatClientAgent) &&
            method.GetParameters()[0].ParameterType.FullName == "OpenAI.Chat.ChatClient" &&
            method.GetParameters().Length == 5);
        methods.Should().ContainSingle(static method =>
            method.ReturnType == typeof(ChatClientAgent) &&
            method.GetParameters()[0].ParameterType.FullName == "OpenAI.Responses.ResponsesClient" &&
            method.GetParameters().Length == 9);
        methods.Should().ContainSingle(static method =>
            method.ReturnType == typeof(ChatClientAgent) &&
            method.GetParameters()[0].ParameterType.FullName == "OpenAI.Responses.ResponsesClient" &&
            method.GetParameters().Length == 6);
        methods.Should().ContainSingle(static method =>
            method.ReturnType == typeof(ChatClientAgent) &&
            method.GetParameters()[0].ParameterType.FullName == "OpenAI.Responses.ResponsesClient" &&
            IsHostedMcpServerToolList(method.GetParameters()[1].ParameterType));
    }

    [Fact]
    public void QylOpenAIClientExtensions_Exposes_Native_OpenAI_Helpers()
    {
        var type = typeof(QylOpenAIClientExtensions);

        AssertSingleByName(type, nameof(QylOpenAIClientExtensions.AsQylOpenAIChatClientWithStoredOutputDisabled), 3, typeof(IChatClient));
        AssertSingleByName(type, nameof(QylOpenAIClientExtensions.AsQylOpenAIChatCompletion), 1, typeof(ChatCompletion));
        AssertSingleByName(type, nameof(QylOpenAIClientExtensions.AsQylResponseResult), 1, typeof(ResponseResult));

        var runAsyncOverloads = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static method => method.Name == nameof(QylOpenAIClientExtensions.RunQylOpenAIAsync))
            .ToArray();
        runAsyncOverloads.Should().HaveCount(2);
        runAsyncOverloads.Should().ContainSingle(static method =>
            method.ReturnType == typeof(Task<ChatCompletion>) &&
            IsEnumerableOf(method.GetParameters()[1].ParameterType, typeof(global::OpenAI.Chat.ChatMessage)));
        runAsyncOverloads.Should().ContainSingle(static method =>
            method.ReturnType == typeof(Task<ResponseResult>) &&
            IsEnumerableOf(method.GetParameters()[1].ParameterType, typeof(global::OpenAI.Responses.ResponseItem)));

        var runStreamingOverloads = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static method => method.Name == nameof(QylOpenAIClientExtensions.RunQylOpenAIStreamingAsync))
            .ToArray();
        runStreamingOverloads.Should().HaveCount(2);
        runStreamingOverloads.Should().ContainSingle(static method =>
            method.ReturnType == typeof(AsyncCollectionResult<StreamingChatCompletionUpdate>) &&
            IsEnumerableOf(method.GetParameters()[1].ParameterType, typeof(global::OpenAI.Chat.ChatMessage)));
        runStreamingOverloads.Should().ContainSingle(static method =>
            method.ReturnType == typeof(AsyncCollectionResult<StreamingResponseUpdate>) &&
            IsEnumerableOf(method.GetParameters()[1].ParameterType, typeof(global::OpenAI.Responses.ResponseItem)));
    }

    private static void AssertSingleByName(Type type, string methodName, int parameterCount, Type returnType) =>
        type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Should()
            .ContainSingle(method =>
                method.Name == methodName &&
                method.ReturnType == returnType &&
                method.GetParameters().Length == parameterCount);

    private static bool IsHostedMcpServerToolList(Type parameterType)
    {
        return parameterType.IsGenericType &&
               parameterType.GetGenericTypeDefinition() == typeof(IList<>) &&
               parameterType.GetGenericArguments()[0].FullName == "Microsoft.Extensions.AI.HostedMcpServerTool";
    }

    private static bool IsEnumerableOf(Type parameterType, Type elementType)
    {
        return parameterType.IsGenericType &&
               parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
               parameterType.GetGenericArguments()[0] == elementType;
    }
}
