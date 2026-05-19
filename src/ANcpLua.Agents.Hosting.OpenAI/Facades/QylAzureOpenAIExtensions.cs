using ANcpLua.Roslyn.Utilities;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Hosting.OpenAI;

/// <summary>
///     Qyl-prefixed Azure OpenAI agent facades. Separate from <see cref="QylOpenAIClientExtensions"/>
///     because <see cref="AzureOpenAIClient"/> is a distinct SDK entry point (different package,
///     different auth surface) — not just an OpenAI-compatible base-URL swap.
/// </summary>
public static class QylAzureOpenAIExtensions
{
    /// <summary>
    ///     Adapts an existing <see cref="AzureOpenAIClient"/> + deployment name to a
    ///     <see cref="ChatClientAgent"/>. Forwards <paramref name="options"/>' telemetry into the
    ///     <see cref="ChatClientAgent"/>; secrets/credentials are already baked into the client.
    /// </summary>
    public static ChatClientAgent AsQylAzureOpenAIAgent(
        this AzureOpenAIClient client,
        string deployment,
        QylAzureOpenAIOptions? options = null,
        string? instructions = null,
        string? name = null,
        IList<AITool>? tools = null,
        IServiceProvider? services = null) =>
        client is null
            ? throw new ArgumentNullException(nameof(client))
            : client.GetChatClient(Guard.NotNullOrWhiteSpace(deployment))
                .AsQylOpenAIAgent(
                    instructions: instructions,
                    name: name,
                    tools: tools,
                    loggerFactory: options?.Telemetry?.LoggerFactory,
                    services: services);

    /// <summary>
    ///     Sugar that builds an <see cref="AzureOpenAIClient"/> from <see cref="QylAzureOpenAIOptions"/>
    ///     (resolving AAD or API-key auth from the record) and then exposes a
    ///     <see cref="ChatClientAgent"/> in one call.
    /// </summary>
    public static ChatClientAgent AsQylAzureOpenAIAgent(
        QylAzureOpenAIOptions options,
        string deployment,
        string? instructions = null,
        string? name = null,
        IList<AITool>? tools = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(options);
        Guard.NotNullOrWhiteSpace(deployment);

        return BuildQylAzureClient(options).AsQylAzureOpenAIAgent(deployment, options, instructions, name, tools, services);
    }

    /// <summary>
    ///     Builds an <see cref="AzureOpenAIClient"/> from <see cref="QylAzureOpenAIOptions"/>.
    ///     Prefers <see cref="QylAzureOpenAIOptions.AzureCredential"/> over
    ///     <see cref="Facades.QylSecrets.ApiKey"/>; either path requires
    ///     <see cref="Facades.QylSecrets.Endpoint"/>.
    /// </summary>
    public static AzureOpenAIClient BuildQylAzureClient(QylAzureOpenAIOptions options)
    {
        Guard.NotNull(options);
        var endpoint = options.Secrets?.Endpoint
            ?? throw new InvalidOperationException("QylAzureOpenAIOptions.Secrets.Endpoint is required.");

        if (options.AzureCredential is { } cred)
            return new AzureOpenAIClient(endpoint, cred);

        if (options.Secrets?.ApiKey is { Length: > 0 } key)
            return new AzureOpenAIClient(endpoint, new AzureKeyCredential(key));

        throw new InvalidOperationException(
            "QylAzureOpenAIOptions requires either AzureCredential or Secrets.ApiKey.");
    }
}
