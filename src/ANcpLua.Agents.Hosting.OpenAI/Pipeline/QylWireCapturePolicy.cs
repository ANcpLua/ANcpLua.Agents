using System.ClientModel.Primitives;
using ANcpLua.Roslyn.Utilities;
using OpenAI;

namespace ANcpLua.Agents.Hosting.OpenAI;

/// <summary>
///     Sibling to <see cref="ClientHeadersPolicy"/> on the OPPOSITE end of the pipeline:
///     <see cref="ClientHeadersPolicy"/> writes outbound headers; this policy observes the full
///     request + response. Useful for model-drift triage and audit captures.
/// </summary>
/// <remarks>
///     The policy is stateless — both the sink delegate and the configuration flags are captured
///     at construction. The sink runs synchronously on the request/response lifecycle thread; if
///     it does I/O, the caller is responsible for queuing.
/// </remarks>
public sealed class QylWireCapturePolicy(
    Action<QylWireCaptureEvent> sink,
    bool captureBody = false) : PipelinePolicy
{
    /// <inheritdoc />
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        Guard.NotNull(message);
        Guard.NotNull(pipeline);
        EmitRequest(message);
        try
        {
            ProcessNext(message, pipeline, currentIndex);
        }
        finally
        {
            EmitResponse(message);
        }
    }

    /// <inheritdoc />
    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        Guard.NotNull(message);
        Guard.NotNull(pipeline);
        EmitRequest(message);
        try
        {
            await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
        }
        finally
        {
            EmitResponse(message);
        }
    }

    private void EmitRequest(PipelineMessage message)
    {
        if (message.Request is not { } request) return;
        string? body = null;
        if (captureBody && request.Content is { } content)
        {
            using var ms = new MemoryStream();
            content.WriteTo(ms);
            body = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        sink(new QylWireCaptureEvent(QylWireCaptureKind.Request, request.Method, request.Uri, body, null));
    }

    private void EmitResponse(PipelineMessage message)
    {
        if (message.Response is not { } response) return;
        string? body = null;
        if (captureBody && response.ContentStream is { } stream && stream.CanRead && stream.CanSeek)
        {
            var origin = stream.Position;
            stream.Position = 0;
            using var reader = new StreamReader(stream, leaveOpen: true);
            body = reader.ReadToEnd();
            stream.Position = origin;
        }
        sink(new QylWireCaptureEvent(QylWireCaptureKind.Response, null, message.Request?.Uri, body, response.Status));
    }
}

/// <summary>Direction of a wire-capture event.</summary>
public enum QylWireCaptureKind
{
    /// <summary>Outbound request observation.</summary>
    Request,
    /// <summary>Inbound response observation.</summary>
    Response,
}

/// <summary>Single wire-capture data point delivered to the sink.</summary>
public sealed record QylWireCaptureEvent(
    QylWireCaptureKind Kind,
    string? Method,
    Uri? Uri,
    string? Body,
    int? StatusCode);

/// <summary>Extensions wiring <see cref="QylWireCapturePolicy"/> into an <see cref="OpenAIClientOptions"/>.</summary>
public static class QylWireCapturePolicyExtensions
{
    /// <summary>
    ///     Registers a <see cref="QylWireCapturePolicy"/> at the per-call pipeline position. Body
    ///     capture is opt-in to keep the default cost negligible for healthy traffic.
    /// </summary>
    public static OpenAIClientOptions AddQylWireCapture(
        this OpenAIClientOptions options,
        Action<QylWireCaptureEvent> sink,
        bool captureBody = false)
    {
        Guard.NotNull(options);
        Guard.NotNull(sink);
        options.AddPolicy(new QylWireCapturePolicy(sink, captureBody), PipelinePosition.PerCall);
        return options;
    }
}
