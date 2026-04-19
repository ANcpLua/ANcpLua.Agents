// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: PortBinding.cs

// Copyright (c) Microsoft. All rights reserved.

namespace ANcpLua.Agents.Testing.Workflows.Internals;

internal class PortBinding(RequestPort port, IExternalRequestSink sink)
{
    public RequestPort Port => port;
    public IExternalRequestSink Sink => sink;

    public ValueTask PostRequestAsync<TRequest>(TRequest request, string? requestId = null,
        CancellationToken cancellationToken = default)
    {
        var externalRequest = ExternalRequest.Create(Port, request, requestId);
        return Sink.PostAsync(externalRequest);
    }
}