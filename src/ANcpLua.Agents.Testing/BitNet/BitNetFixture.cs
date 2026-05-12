using ANcpLua.Agents.Hosting.BitNet;
using Microsoft.Extensions.AI;
using Xunit;

namespace ANcpLua.Agents.Testing.BitNet;

/// <summary>
///     Shared fixture that probes a BitNet (<c>bitnet.cpp</c> <c>llama-server</c>) instance and
///     exposes an <see cref="IChatClient" />. Tests should guard with
///     <c>Skip.IfNot(bitnet.IsAvailable, "BitNet not running")</c>.
/// </summary>
/// <remarks>
///     <para>Configuration mirrors <see cref="QylBitNetClientOptions" />:</para>
///     <list type="bullet">
///         <item><c>BITNET_URL</c> overrides the default <c>http://localhost:8080</c> endpoint.</item>
///         <item><c>BITNET_API_PATH</c> overrides the OpenAI-compatible API path (default <c>/v1</c>).</item>
///         <item><c>BITNET_MODEL</c> overrides the model identifier.</item>
///     </list>
///     <para>The fixture probes <c>/health</c> with a 3-second timeout during
///     <see cref="InitializeAsync" />; when reachable, the runtime <see cref="IChatClient" /> is
///     built via <see cref="QylBitNetChatClientFactory.Create" /> (including the
///     <c>LegacyMaxTokensPolicy</c> shim for pre-#19831 <c>llama-server</c> builds).</para>
/// </remarks>
public sealed class BitNetFixture : IAsyncLifetime
{
    private static readonly Uri s_defaultEndpoint = new("http://localhost:8080");

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <summary>
    ///     Chat client connected to the BitNet server. Only usable when <see cref="IsAvailable" />
    ///     is <see langword="true" />.
    /// </summary>
    public IChatClient? ChatClient { get; private set; }

    /// <summary>
    ///     Whether the BitNet server responded to the health probe during initialization.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var options = new QylBitNetClientOptions { Endpoint = s_defaultEndpoint }.ApplyEnvironmentOverrides();
        var endpoint = options.Endpoint ?? s_defaultEndpoint;

        try
        {
            using var cts = new CancellationTokenSource(options.HealthProbeTimeout);
            using var response = await _http.GetAsync(new Uri(endpoint, "/health"), cts.Token)
                .ConfigureAwait(false);
            IsAvailable = response.IsSuccessStatusCode;
        }
        catch
        {
            IsAvailable = false;
        }

        if (!IsAvailable) return;

        ChatClient = QylBitNetChatClientFactory.Create(options);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        ChatClient?.Dispose();
        return ValueTask.CompletedTask;
    }
}
