// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Agents.Testing.Conformance.Telemetry;

/// <summary>
///     Per-provider fixture extension for telemetry conformance. A consumer fixture combines
///     <see cref="IAgentFixture" /> with <see cref="ITelemetryAssertingFixture" /> to declare
///     the <see cref="System.Diagnostics.ActivitySource" /> name(s) the provider's pipeline
///     emits — so <see cref="TelemetryConformanceTests{TFixture}" /> can attach an
///     <see cref="Diagnostics.ActivityCollector" /> around the agent run and assert at least
///     one activity is observed.
/// </summary>
public interface ITelemetryAssertingFixture
{
    /// <summary>
    ///     Names of <see cref="System.Diagnostics.ActivitySource" />s expected to emit during
    ///     a normal agent run on this fixture. For qyl this is typically <c>["qyl.agent"]</c>;
    ///     for raw MAF it might be <c>["Microsoft.Extensions.AI"]</c>.
    /// </summary>
    IReadOnlyCollection<string> ExpectedActivitySources { get; }
}
