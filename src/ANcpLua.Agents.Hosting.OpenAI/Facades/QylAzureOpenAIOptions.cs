using ANcpLua.Agents.Facades;
using Azure.Core;

namespace ANcpLua.Agents.Hosting.OpenAI;

/// <summary>
///     Options record for <c>AsQylAzureOpenAIAgent</c>. Composes the Qyl-canonical
///     <see cref="QylSecrets"/> + <see cref="QylTelemetry"/> records with an Azure-specific
///     <see cref="TokenCredential"/> slot for AAD authentication paths.
/// </summary>
/// <remarks>
///     Resolution order when the caller supplies the options to a client-factory overload:
///     <list type="number">
///         <item><description><see cref="AzureCredential"/> + <see cref="QylSecrets.Endpoint"/> — preferred AAD path.</description></item>
///         <item><description><see cref="QylSecrets.ApiKey"/> + <see cref="QylSecrets.Endpoint"/> — key path.</description></item>
///     </list>
/// </remarks>
public sealed record QylAzureOpenAIOptions
{
    /// <summary>Qyl-canonical secrets bag (ApiKey + Endpoint).</summary>
    public QylSecrets? Secrets { get; init; }

    /// <summary>Azure-specific token credential for AAD authentication.</summary>
    public TokenCredential? AzureCredential { get; init; }

    /// <summary>Telemetry handle (logger factory + tracer).</summary>
    public QylTelemetry? Telemetry { get; init; }
}
