// Licensed to the .NET Foundation under one or more agreements.

using System.Diagnostics;
using ANcpLua.Agents.Testing.Hosting.Flavors;
using Xunit;

namespace ANcpLua.Agents.Testing.Hosting;

/// <summary>
///     Tier-1 host-conformance suite. Builds the consumer-supplied <see cref="AgentHostPipeline" />
///     through every <see cref="IHostFlavor" /> in <see cref="AllTier1HostFlavors" /> and asserts
///     host-independent invariants on the resulting <see cref="IServiceProvider" />.
///     <para>
///         Tier-1 is DI-walk only — no agent run, no <see cref="System.Net.Http.HttpClient" />
///         traffic, no fake chat client. That keeps every test under a few hundred milliseconds
///         and makes failures bisect cleanly to "the pipeline doesn't register X" vs.
///         "host shape Y interferes with X".
///     </para>
///     <para>
///         Inherit and override <see cref="Pipeline" />, <see cref="ExpectedSingletons" />, and
///         optionally <see cref="ExpectedActivitySource" /> to bind the suite to a concrete
///         consumer setup.
///     </para>
/// </summary>
/// <example>
///     <code>
///         public sealed class MyAgentHostingConformance : HostingTier1ConformanceTests
///         {
///             protected override AgentHostPipeline Pipeline =&gt; services =&gt;
///                 services.AddAIAgent("triage", "You triage incoming tickets.")
///                         .WithMyTelemetry();
///
///             protected override IReadOnlyCollection&lt;Type&gt; ExpectedSingletons =&gt;
///                 [typeof(IMyRunStateStore)];
///
///             protected override string? ExpectedActivitySource =&gt; "my.agent";
///         }
///     </code>
/// </example>
public abstract class HostingTier1ConformanceTests
{
    /// <summary>Pipeline under test. Must be deterministic — invoked multiple times per test run.</summary>
    protected abstract AgentHostPipeline Pipeline { get; }

    /// <summary>Singleton service types expected to resolve from the built provider.</summary>
    protected abstract IReadOnlyCollection<Type> ExpectedSingletons { get; }

    /// <summary>
    ///     Optional <see cref="ActivitySource" /> name expected to be registered. Override and
    ///     return non-null to engage the source-created assert. Default: <c>null</c> (skipped).
    /// </summary>
    protected virtual string? ExpectedActivitySource => null;

    /// <summary>
    ///     Build the pipeline through every Tier-1 host flavor. Assert each expected singleton
    ///     resolves and is the same instance across two resolutions (i.e. registered as singleton,
    ///     not transient).
    /// </summary>
    [Theory]
    [ClassData(typeof(AllTier1HostFlavors))]
    public async Task PipelineRegistersExpectedSingletonsAsync(IHostFlavor host)
    {
        // Arrange / Act
        await using var handle = host.Build(Pipeline);

        // Assert
        foreach (var serviceType in ExpectedSingletons)
        {
            var first = handle.Services.GetService(serviceType);
            Assert.True(
                first is not null,
                $"[{handle.FlavorName}] Expected singleton of type '{serviceType.FullName}' to be registered, but resolved null.");

            var second = handle.Services.GetService(serviceType);
            Assert.Same(first, second);
        }
    }

    /// <summary>
    ///     Apply the pipeline twice to a single service collection and assert no duplicate
    ///     singleton registrations result. This catches pipelines that omit
    ///     <c>TryAddSingleton</c> guards on shared infrastructure.
    /// </summary>
    [Theory]
    [ClassData(typeof(AllTier1HostFlavors))]
    public async Task PipelineIsIdempotentAsync(IHostFlavor host)
    {
        // Arrange
        AgentHostPipeline doubled = services =>
        {
            Pipeline(services);
            Pipeline(services);
        };

        // Act
        await using var handle = host.Build(doubled);

        // Assert
        foreach (var serviceType in ExpectedSingletons)
        {
            var first = handle.Services.GetService(serviceType);
            var second = handle.Services.GetService(serviceType);
            Assert.NotNull(first);
            Assert.Same(first, second);
        }
    }

    /// <summary>
    ///     If <see cref="ExpectedActivitySource" /> is set, assert that the
    ///     <see cref="ActivitySource" /> with that name has been created in-process by the time
    ///     the pipeline finishes building. Verified by attaching an <see cref="ActivityListener" />
    ///     after build — the runtime invokes <c>ShouldListenTo</c> once for every previously
    ///     created source, so a name match flips the observed flag.
    /// </summary>
    [Theory]
    [ClassData(typeof(AllTier1HostFlavors))]
    public async Task PipelineRegistersExpectedActivitySourceAsync(IHostFlavor host)
    {
        // Arrange
        if (ExpectedActivitySource is null)
            return;

        await using var handle = host.Build(Pipeline);

        // Act
        var sourceCreated = false;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source =>
            {
                if (string.Equals(source.Name, ExpectedActivitySource, StringComparison.Ordinal))
                    sourceCreated = true;
                return false;
            },
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.None
        };
        ActivitySource.AddActivityListener(listener);

        // Assert
        Assert.True(
            sourceCreated,
            $"[{handle.FlavorName}] Expected ActivitySource '{ExpectedActivitySource}' to be created during pipeline build.");
    }
}
