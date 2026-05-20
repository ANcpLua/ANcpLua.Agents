using System.ClientModel;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;
using OpenAI;

namespace ANcpLua.Agents.Hosting.OpenAI;

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
///     on agent availability. The returned client always supports per-call <c>x-client-*</c>
///     headers via <see cref="ClientHeadersScope.WithClientHeader"/> — the policy and decorator
///     are no-ops when no headers are attached, so the feature costs nothing at the call site
///     until used.
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

        var clientOptions = new OpenAIClientOptions();
        if (options.Endpoint is not null)
            clientOptions.Endpoint = new Uri(options.Endpoint);
        clientOptions.AddClientHeadersPolicy();

        var openAiClient = new OpenAIClient(credential, clientOptions);
        return new ClientHeadersChatClient(openAiClient.GetChatClient(model).AsIChatClient());
    }

    /// <summary>
    ///     Builds an <see cref="IChatClient"/> from environment variables under
    ///     <paramref name="prefix"/>: <c>{prefix}_API_KEY</c>, <c>{prefix}_MODEL</c>,
    ///     <c>{prefix}_ENDPOINT</c>. Returns <c>null</c> when no API key is set.
    ///     The default prefix is <c>ANCPLUA_AGENT</c>.
    /// </summary>
    /// <param name="prefix">
    ///     Environment-variable prefix without trailing underscore. Consumers that
    ///     ship their own envvar conventions (e.g. <c>QYL_AGENT</c>) pass the prefix
    ///     here instead of duplicating this factory body.
    /// </param>
    public static IChatClient? TryCreateFromEnvironment(string prefix = "ANCPLUA_AGENT") =>
        TryCreate(new AgentChatClientOptions(
            ApiKey: Environment.GetEnvironmentVariable($"{prefix}_API_KEY"),
            Model: Environment.GetEnvironmentVariable($"{prefix}_MODEL"),
            Endpoint: Environment.GetEnvironmentVariable($"{prefix}_ENDPOINT")));
}
