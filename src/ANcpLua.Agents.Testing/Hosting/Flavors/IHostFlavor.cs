// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Agents.Testing.Hosting.Flavors;

/// <summary>
///     Boundary between a consumer-defined <see cref="AgentHostPipeline" /> and the host shape
///     that owns its service collection. Each implementation builds a fresh
///     <see cref="IServiceProvider" /> from the same pipeline so conformance tests can replay
///     identical registrations across host families and assert host-independent invariants.
/// </summary>
public interface IHostFlavor
{
    /// <summary>Display name surfaced in xUnit theory output and assertion messages.</summary>
    string Name { get; }

    /// <summary>Build a fresh host, applying the given pipeline.</summary>
    /// <param name="pipeline">Pipeline that mutates the host's service collection.</param>
    /// <returns>A handle whose disposal tears down the host and provider.</returns>
    IHostHandle Build(AgentHostPipeline pipeline);
}
