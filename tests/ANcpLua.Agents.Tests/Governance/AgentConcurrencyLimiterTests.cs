using ANcpLua.Agents.Governance;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Agents.Tests.Governance;

public sealed class AgentConcurrencyLimiterTests
{
    [Fact]
    public async Task AcquireAsync_QueuesWhenAtLimit_ReleasesInOrder()
    {
        using var limiter = new AgentConcurrencyLimiter(defaultLimit: 2);
        var slot1 = await limiter.AcquireAsync("tool");
        var slot2 = await limiter.AcquireAsync("tool");

        limiter.GetInUseCount("tool").Should().Be(2);
        limiter.GetAvailableSlots("tool").Should().Be(0);

        var pending = limiter.AcquireAsync("tool").AsTask();
        pending.IsCompleted.Should().BeFalse();

        await slot1.DisposeAsync();
        var slot3 = await pending;

        limiter.GetInUseCount("tool").Should().Be(2);

        await slot2.DisposeAsync();
        await slot3.DisposeAsync();

        limiter.GetInUseCount("tool").Should().Be(0);
    }

    [Fact]
    public async Task AcquireAsync_WithPolicy_UsesPolicyLimit()
    {
        using var limiter = new AgentConcurrencyLimiter(defaultLimit: 100);
        var policy = new AgentToolPolicy(MaxAttempts: 1, MaxToolCalls: 1, RequiredCapabilities: [], RequiresApproval: false);

        var first = await limiter.AcquireAsync("t", policy);
        var pending = limiter.AcquireAsync("t", policy).AsTask();

        pending.IsCompleted.Should().BeFalse();
        await first.DisposeAsync();
        var second = await pending;
        await second.DisposeAsync();
    }
}
