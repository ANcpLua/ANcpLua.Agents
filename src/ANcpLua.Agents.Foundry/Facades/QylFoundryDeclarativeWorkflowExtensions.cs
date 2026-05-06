using ANcpLua.Roslyn.Utilities;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Core;
using Microsoft.Agents.AI.Workflows.Declarative;

namespace ANcpLua.Agents.Foundry;

/// <summary>
///     <c>Qyl</c>-prefixed factory for the
///     <c>Microsoft.Agents.AI.Workflows.Declarative.Foundry</c> bridge that wires
///     <see cref="AzureAgentProvider" /> into a <see cref="DeclarativeWorkflowOptions" />
///     in a single call.
/// </summary>
public static class QylFoundryDeclarativeWorkflowExtensions
{
    /// <summary>
    ///     Builds a <see cref="DeclarativeWorkflowOptions" /> driven by a Foundry
    ///     <see cref="AzureAgentProvider" /> bound to <paramref name="projectEndpoint" />
    ///     and <paramref name="credential" />. Optional init-properties on the provider
    ///     are forwarded as method arguments.
    /// </summary>
    public static DeclarativeWorkflowOptions BuildQylFoundryDeclarativeWorkflowOptions(
        Uri projectEndpoint,
        TokenCredential credential,
        AIProjectClientOptions? projectClientOptions = null,
        ProjectOpenAIClientOptions? openAIClientOptions = null,
        HttpClient? httpClient = null)
    {
        Guard.NotNull(projectEndpoint);
        Guard.NotNull(credential);

        AzureAgentProvider provider = new(projectEndpoint, credential)
        {
            AIProjectClientOptions = projectClientOptions,
            OpenAIClientOptions = openAIClientOptions,
            HttpClient = httpClient,
        };

        return new DeclarativeWorkflowOptions(provider);
    }
}
