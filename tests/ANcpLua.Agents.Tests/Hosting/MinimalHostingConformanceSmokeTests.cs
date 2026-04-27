using System.Diagnostics;
using ANcpLua.Agents.Testing.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Tests.Hosting;

/// <summary>
///     Smoke test for <see cref="HostingTier1ConformanceTests" />: derives the abstract suite
///     with a trivial pipeline (one singleton + one ActivitySource) and expects every theory
///     row across every <c>IHostFlavor</c> to pass. Failure here means the suite skeleton is
///     broken — not the consumer pipeline.
/// </summary>
public sealed class MinimalHostingConformanceSmokeTests : HostingTier1ConformanceTests
{
    private static readonly ActivitySource s_smokeSource = new("test.hosting.smoke");

    /// <inheritdoc />
    protected override AgentHostPipeline Pipeline => static services =>
    {
        services.AddSingleton<DummySingleton>();
        // Touch the source so the runtime registers it before the listener attaches.
        _ = s_smokeSource.HasListeners();
    };

    /// <inheritdoc />
    protected override IReadOnlyCollection<Type> ExpectedSingletons =>
        [typeof(DummySingleton)];

    /// <inheritdoc />
    protected override string? ExpectedActivitySource => "test.hosting.smoke";

    private sealed class DummySingleton;
}
