using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ANcpLua.Agents.Hosting.BitNet;

/// <summary>
///     Probes the <c>/health</c> endpoint of a <c>bitnet.cpp</c> <c>llama-server</c> with the
///     timeout from <see cref="QylBitNetClientOptions.HealthProbeTimeout" />.
/// </summary>
/// <remarks>
///     Resolves named options via <see cref="IOptionsMonitor{TOptions}.Get(string)" /> so a single
///     health-check service can probe many endpoints registered under different connection names.
///     The shared <see cref="HttpClient" /> is short-lived (per-probe) because <c>llama-server</c>
///     does not keep-alive long idle TCP connections reliably.
/// </remarks>
public sealed class QylBitNetHealthCheck : IHealthCheck
{
    private readonly string _connectionName;
    private readonly IOptionsMonitor<QylBitNetClientOptions> _optionsMonitor;

    /// <summary>
    ///     Creates a health-check bound to the options registered under
    ///     <paramref name="connectionName" />.
    /// </summary>
    public QylBitNetHealthCheck(string connectionName, IOptionsMonitor<QylBitNetClientOptions> optionsMonitor)
    {
        Guard.NotNullOrWhiteSpace(connectionName);
        Guard.NotNull(optionsMonitor);

        _connectionName = connectionName;
        _optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.Get(_connectionName);
        if (options.Endpoint is null)
            return HealthCheckResult.Unhealthy($"BitNet endpoint '{_connectionName}' is not configured.");

        using var http = new HttpClient { Timeout = options.HealthProbeTimeout };
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(options.HealthProbeTimeout);
            using var response = await http.GetAsync(new Uri(options.Endpoint, "/health"), cts.Token)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"BitNet '{_connectionName}' responded {(int)response.StatusCode}.")
                : HealthCheckResult.Unhealthy($"BitNet '{_connectionName}' responded {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy($"BitNet '{_connectionName}' probe failed.", ex);
        }
    }
}
