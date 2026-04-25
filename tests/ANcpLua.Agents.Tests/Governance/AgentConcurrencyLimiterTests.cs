using ANcpLua.Agents.Governance;

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
        var policy = new AgentToolPolicy(MaxAttempts: 1, MaxToolCalls: 1, RequiredCapabilities: []);

        var first = await limiter.AcquireAsync("t", policy);
        var pending = limiter.AcquireAsync("t", policy).AsTask();

        pending.IsCompleted.Should().BeFalse();
        await first.DisposeAsync();
        var second = await pending;
        await second.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_DoubleDispose_DoesNotOverRelease()
    {
        using var limiter = new AgentConcurrencyLimiter(defaultLimit: 1);
        var slot = await limiter.AcquireAsync("t");

        await slot.DisposeAsync();
        await slot.DisposeAsync();

        limiter.GetAvailableSlots("t").Should().Be(1);
    }

    [Fact]
    public void GetAvailableSlots_UnseenTool_ReturnsDefaultLimit()
    {
        using var limiter = new AgentConcurrencyLimiter(defaultLimit: 7);

        limiter.GetAvailableSlots("never-touched").Should().Be(7);
        limiter.GetInUseCount("never-touched").Should().Be(0);
    }

    [Fact]
    public async Task Reset_PerTool_AllowsReAcquireAfterRelease()
    {
        using var limiter = new AgentConcurrencyLimiter(defaultLimit: 1);
        var slot = await limiter.AcquireAsync("t");
        await slot.DisposeAsync();

        limiter.Reset("t");

        var fresh = await limiter.AcquireAsync("t");
        fresh.Should().NotBeNull();
        limiter.GetInUseCount("t").Should().Be(1);

        await fresh.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var limiter = new AgentConcurrencyLimiter(defaultLimit: 1);
        limiter.Dispose();

        var act = async () => await limiter.AcquireAsync("t");

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AcquireAsync_BlankToolName_ThrowsArgumentException(string toolName)
    {
        using var limiter = new AgentConcurrencyLimiter(defaultLimit: 1);

        var act = async () => await limiter.AcquireAsync(toolName);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Ctor_DefaultLimitBelowOne_Throws()
    {
        var act = static () => new AgentConcurrencyLimiter(defaultLimit: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Dispose_IsIdempotent()
    {
        var limiter = new AgentConcurrencyLimiter(defaultLimit: 1);
        var slot = await limiter.AcquireAsync("t");
        await slot.DisposeAsync();

        limiter.Dispose();
        limiter.Dispose();
    }

    [Fact]
    public async Task AcquireAsync_PolicyWithZeroToolCalls_FallsBackToDefault()
    {
        using var limiter = new AgentConcurrencyLimiter(defaultLimit: 2);
        var policy = new AgentToolPolicy(MaxAttempts: 1, MaxToolCalls: 0, RequiredCapabilities: []);

        var first = await limiter.AcquireAsync("t", policy);
        var second = await limiter.AcquireAsync("t", policy);

        limiter.GetInUseCount("t").Should().Be(2);

        await first.DisposeAsync();
        await second.DisposeAsync();
    }

    [Fact]
    public async Task GetInUseCount_RespectsPerPolicySize()
    {
        using var limiter = new AgentConcurrencyLimiter(defaultLimit: 100);
        var policy = new AgentToolPolicy(MaxAttempts: 99, MaxToolCalls: 3, RequiredCapabilities: []);

        var first = await limiter.AcquireAsync("t", policy);
        var second = await limiter.AcquireAsync("t", policy);

        limiter.GetInUseCount("t").Should().Be(2);

        await first.DisposeAsync();
        await second.DisposeAsync();
    }
}
