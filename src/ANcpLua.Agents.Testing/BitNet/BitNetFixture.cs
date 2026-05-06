using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using Xunit;

namespace ANcpLua.Agents.Testing.BitNet;

/// <summary>
///     Shared fixture that probes a BitNet (llama.cpp) server and exposes an <see cref="IChatClient" />.
///     Tests should guard with <c>Skip.IfNot(bitnet.IsAvailable, "BitNet not running")</c>.
/// </summary>
/// <remarks>
///     <para>Configuration:</para>
///     <list type="bullet">
///         <item><c>BITNET_URL</c> env var overrides the default <c>http://localhost:8080</c> endpoint.</item>
///         <item><c>BITNET_API_PATH</c> env var overrides the default OpenAI-compatible API path.</item>
///         <item><c>BITNET_MODEL</c> env var overrides the default model id.</item>
///         <item>The fixture probes <c>/health</c> with a 3-second timeout during <see cref="InitializeAsync" />.</item>
///     </list>
/// </remarks>
public sealed class BitNetFixture : IAsyncLifetime
{
    private const string DefaultApiPath = "/v1";
    private const string DefaultModel = "bitnet-b1.58-2B-4T";
    private const string UnusedApiKey = "unused";

    private static readonly Uri s_defaultEndpoint = new("http://localhost:8080");

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <summary>
    ///     Chat client connected to the BitNet server. Only usable when <see cref="IsAvailable" /> is
    ///     <see langword="true" />.
    /// </summary>
    public IChatClient? ChatClient { get; private set; }

    /// <summary>
    ///     Whether the BitNet server responded to the health probe during initialization.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("BITNET_URL") is { Length: > 0 } url
            ? new Uri(url)
            : s_defaultEndpoint;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var response = await _http.GetAsync(new Uri(endpoint, "/health"), cts.Token)
                .ConfigureAwait(false);
            IsAvailable = response.IsSuccessStatusCode;
        }
        catch
        {
            IsAvailable = false;
        }

        if (!IsAvailable) return;

        var apiPath = Environment.GetEnvironmentVariable("BITNET_API_PATH") is { Length: > 0 } configuredApiPath
            ? configuredApiPath
            : DefaultApiPath;
        var model = Environment.GetEnvironmentVariable("BITNET_MODEL") is { Length: > 0 } configuredModel
            ? configuredModel
            : DefaultModel;

        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint, apiPath) };
        var client = new OpenAIClient(new ApiKeyCredential(UnusedApiKey), options);
        ChatClient = client.GetChatClient(model).AsIChatClient();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        ChatClient?.Dispose();
        return ValueTask.CompletedTask;
    }
}
