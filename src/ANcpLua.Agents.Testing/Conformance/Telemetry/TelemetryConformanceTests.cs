// Licensed to the .NET Foundation under one or more agreements.

using Xunit;

namespace ANcpLua.Agents.Testing.Conformance.Telemetry;

/// <summary>
///     Provider-agnostic conformance for OTel emission during an agent run. Combines
///     <see cref="IAgentFixture" /> (to actually run an agent against a real or scripted
///     provider) with <see cref="ITelemetryAssertingFixture" /> (to declare which
///     <see cref="System.Diagnostics.ActivitySource" />s the consumer pipeline registers).
///     <para>
///         Asserts only the structural invariant: a normal run emits at least one activity
///         from at least one expected source. Domain-specific assertions about tag values,
///         span hierarchy, or lineage IDs belong in consumer-owned suites — those are exactly
///         the per-pipeline shape that this conformance layer can not assume.
///     </para>
/// </summary>
/// <typeparam name="TFixture">Concrete fixture combining the two contracts.</typeparam>
public abstract class TelemetryConformanceTests<TFixture>(Func<TFixture> createFixture)
    : AgentTestBase<TFixture>(createFixture)
    where TFixture : IAgentFixture, ITelemetryAssertingFixture
{
    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunEmitsAtLeastOneActivityFromExpectedSourceAsync()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sources = Fixture.ExpectedActivitySources;
        Assert.NotEmpty(sources);
        using var captured = CapturedTelemetry.FromSources([.. sources]);

        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);

        // Act
        var response = await agent.RunAsync(
            "Emit at least one telemetry span please.",
            session,
            cancellationToken: ct).ConfigureAwait(false);
        await Fixture.DeleteSessionAsync(session).ConfigureAwait(false);

        // Assert
        Assert.NotNull(response);
        Assert.True(
            captured.StoppedActivities.Count > 0,
            $"Expected at least one stopped Activity from sources [{string.Join(", ", sources)}], captured {captured.StoppedActivities.Count}.");
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task EveryCapturedActivityHasNonEmptyOperationNameAsync()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sources = Fixture.ExpectedActivitySources;
        using var captured = CapturedTelemetry.FromSources([.. sources]);

        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);

        // Act
        await agent.RunAsync("ping", session, cancellationToken: ct).ConfigureAwait(false);
        await Fixture.DeleteSessionAsync(session).ConfigureAwait(false);

        // Assert
        Assert.All(
            captured.StoppedActivities,
            activity => Assert.False(string.IsNullOrEmpty(activity.OperationName)));
    }
}
