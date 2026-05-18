using System.ClientModel.Primitives;
using ANcpLua.Roslyn.Utilities;
using OpenAI;

namespace ANcpLua.Agents.Hosting.OpenAI;

/// <summary>
///     <see cref="PipelinePolicy"/> that stamps the per-call <c>x-client-*</c> headers from
///     <see cref="ClientHeadersScope.Current"/> onto the outbound <see cref="PipelineRequest"/>.
///     Stateless singleton — header values are read live from the AsyncLocal scope on every
///     invocation, so concurrent calls on the same <see cref="OpenAIClient"/> independently
///     stamp their own identity. No-op when no scope is active.
/// </summary>
public sealed class ClientHeadersPolicy : PipelinePolicy
{
    /// <summary>Singleton instance; the policy is stateless.</summary>
    public static ClientHeadersPolicy Instance { get; } = new();

    private ClientHeadersPolicy() { }

    /// <inheritdoc />
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        Guard.NotNull(message);
        Guard.NotNull(pipeline);

        Stamp(message);
        ProcessNext(message, pipeline, currentIndex);
    }

    /// <inheritdoc />
    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        Guard.NotNull(message);
        Guard.NotNull(pipeline);

        Stamp(message);
        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
    }

    private static void Stamp(PipelineMessage message)
    {
        if (ClientHeadersScope.Current is not { Count: > 0 } headers) return;
        if (message.Request is not { } request) return;

        foreach (var (name, value) in headers)
            request.Headers.Set(name, value);
    }
}

/// <summary>Extensions wiring <see cref="ClientHeadersPolicy"/> into an <see cref="OpenAIClientOptions"/>.</summary>
public static class ClientHeadersPolicyExtensions
{
    /// <summary>
    ///     Registers <see cref="ClientHeadersPolicy"/> at the per-call pipeline position so the
    ///     headers stamp once per logical request (header identity is invariant across SDK
    ///     retries). Call once during <see cref="OpenAIClient"/> construction; the policy is
    ///     stateless and reads its values from the AsyncLocal scope at request time.
    /// </summary>
    public static OpenAIClientOptions AddClientHeadersPolicy(this OpenAIClientOptions options)
    {
        Guard.NotNull(options);
        options.AddPolicy(ClientHeadersPolicy.Instance, PipelinePosition.PerCall);
        return options;
    }
}
