// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/IExternalRequestSink.cs

// Copyright (c) Microsoft. All rights reserved.

namespace ANcpLua.Agents.Testing.Workflows.Internals;

internal interface IExternalRequestSink
{
    ValueTask PostAsync(ExternalRequest request);
}