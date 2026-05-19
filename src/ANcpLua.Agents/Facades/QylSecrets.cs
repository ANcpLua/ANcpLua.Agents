using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Agents.Facades;

/// <summary>
///     Qyl-canonical credential record. Each hosting facade accepts <see cref="QylSecrets"/>
///     plus its own provider-specific extension (e.g. <c>TokenCredential</c> for Azure) so
///     callers can share one secrets shape across multiple providers.
/// </summary>
public sealed record QylSecrets
{
    /// <summary>API key, when authenticating via key.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Endpoint URL, when overriding the provider default.</summary>
    public Uri? Endpoint { get; init; }
}

/// <summary>
///     Qyl-canonical observability handle. Pair with <see cref="QylSecrets"/> on hosting facades
///     so a single options record carries both identity and telemetry plumbing.
/// </summary>
public sealed record QylTelemetry
{
    /// <summary>Logger factory passed down to <c>ChatClientAgent</c> construction.</summary>
    public ILoggerFactory? LoggerFactory { get; init; }

    /// <summary>ActivitySource consumed by <c>UseQylTracing</c> / <c>TracedAIFunction</c>.</summary>
    public ActivitySource? Tracer { get; init; }
}
