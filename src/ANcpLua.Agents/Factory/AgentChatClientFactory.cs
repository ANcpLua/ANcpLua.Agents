using System.ClientModel;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;
using OpenAI;

namespace ANcpLua.Agents.Factory;

/// <summary>
///     Configuration for <see cref="AgentChatClientFactory.TryCreate"/>.
/// </summary>
/// <param name="ApiKey">LLM API key. When <c>null</c> or empty the factory returns <c>null</c>.</param>
/// <param name="Model">Model name. Defaults to <c>gpt-4o</c>.</param>
/// <param name="Endpoint">
///     Optional OpenAI-compatible endpoint URL. Set this for Ollama, Anthropic-via-proxy,
///     Azure OpenAI, or any other OpenAI-compatible backend.
/// </param>
public sealed record AgentChatClientOptions(string? ApiKey, string? Model = null, string? Endpoint = null);

/// <summary>
///     Creates an <see cref="IChatClient"/> from <see cref="AgentChatClientOptions"/> using the
///     OpenAI .NET SDK. Returns <c>null</c> when no API key is configured so callers can branch
///     on agent availability.
/// </summary>
public static class AgentChatClientFactory
{
    /// <summary>
    ///     Builds an <see cref="IChatClient"/> from <paramref name="options"/>; returns <c>null</c>
    ///     when <see cref="AgentChatClientOptions.ApiKey"/> is missing.
    /// </summary>
    public static IChatClient? TryCreate(AgentChatClientOptions options)
    {
        Guard.NotNull(options);

        if (string.IsNullOrEmpty(options.ApiKey))
            return null;

        var model = string.IsNullOrEmpty(options.Model) ? "gpt-4o" : options.Model;
        var credential = new ApiKeyCredential(options.ApiKey);

        var openAiClient = options.Endpoint is not null
            ? new OpenAIClient(credential, new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) })
            : new OpenAIClient(credential);

        return openAiClient.GetChatClient(model).AsIChatClient();
    }

    /// <summary>
    ///     Builds an <see cref="IChatClient"/> from the standard <c>ANCPLUA_AGENT_*</c> environment
    ///     variables: <c>ANCPLUA_AGENT_API_KEY</c>, <c>ANCPLUA_AGENT_MODEL</c>,
    ///     <c>ANCPLUA_AGENT_ENDPOINT</c>. Returns <c>null</c> when no API key is set.
    /// </summary>
    public static IChatClient? TryCreateFromEnvironment() =>
        TryCreate(new AgentChatClientOptions(
            ApiKey: Environment.GetEnvironmentVariable("ANCPLUA_AGENT_API_KEY"),
            Model: Environment.GetEnvironmentVariable("ANCPLUA_AGENT_MODEL"),
            Endpoint: Environment.GetEnvironmentVariable("ANCPLUA_AGENT_ENDPOINT")));
}
