using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;
using ANcpLua.Roslyn.Utilities;
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

        // Defensive shim for older OpenAI-compat servers: OpenAI deprecated
        // `max_tokens` in favor of `max_completion_tokens` (Sept 2024, alongside
        // o1 reasoning models), and the .NET SDK only emits the new field. Any
        // llama-server build older than ggml-org/llama.cpp PR #19831 (merged
        // 2026-02-23) silently ignores `max_completion_tokens` and runs to the
        // context limit. Register a per-call policy on the inherited
        // ClientPipelineOptions so both fields end up on the wire.
        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint, apiPath) };
        options.AddPolicy(new LegacyMaxTokensPolicy(), PipelinePosition.PerCall);
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

/// <summary>
///     Mirrors <c>max_completion_tokens</c> → <c>max_tokens</c> in the outbound
///     chat-completion JSON body. Older OpenAI-compat servers (anything before
///     ggml-org/llama.cpp PR #19831, merged 2026-02-23) only honor the legacy
///     <c>max_tokens</c> field; the .NET SDK only emits the new one. Without this
///     mirror the server ignores the cap and generates until the context fills.
/// </summary>
/// <remarks>
///     Registered on <see cref="OpenAIClientOptions" /> via the inherited
///     <c>ClientPipelineOptions.AddPolicy</c> hook so the policy runs after the
///     SDK has serialized the <see cref="PipelineRequest.Content" /> — we parse
///     the final JSON body, copy the field, and rewrite the content. The async
///     path uses <c>WriteToAsync</c> end-to-end so the pipeline never blocks on
///     sync I/O. Self-deleting: once the target server accepts
///     <c>max_completion_tokens</c> natively, this becomes a no-op.
/// </remarks>
internal sealed class LegacyMaxTokensPolicy : PipelinePolicy
{
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        Mirror(message);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        await MirrorAsync(message).ConfigureAwait(false);
        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
    }

    private static void Mirror(PipelineMessage message)
    {
        if (!ShouldMirror(message, out var content)) return;
        using var buffer = new MemoryStream();
        content.WriteTo(buffer, default);
        Apply(message, buffer);
    }

    private static async ValueTask MirrorAsync(PipelineMessage message)
    {
        if (!ShouldMirror(message, out var content)) return;
        await using var buffer = new MemoryStream();
        await content.WriteToAsync(buffer, message.CancellationToken).ConfigureAwait(false);
        Apply(message, buffer);
    }

    private static bool ShouldMirror(PipelineMessage message, out BinaryContent content)
    {
        content = null!;
        if (message.Request?.Content is not { } c) return false;
        // OpenAI's spec defines the path as `/v1/chat/completions` (lowercase),
        // and llama-server preserves casing; an ordinal compare is sufficient.
        if (message.Request.Uri?.AbsolutePath?.EndsWithOrdinal("/chat/completions") is not true) return false;
        content = c;
        return true;
    }

    private static void Apply(PipelineMessage message, MemoryStream buffer)
    {
        buffer.Position = 0;
        if (JsonNode.Parse(buffer) is not JsonObject body) return;
        if (body["max_tokens"] is null && body["max_completion_tokens"] is { } mct)
        {
            body["max_tokens"] = mct.DeepClone();
            message.Request.Content = BinaryContent.Create(BinaryData.FromString(body.ToJsonString()));
        }
    }
}
