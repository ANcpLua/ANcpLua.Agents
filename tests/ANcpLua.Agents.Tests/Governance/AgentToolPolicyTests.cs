using ANcpLua.Agents.Governance;

namespace ANcpLua.Agents.Tests.Governance;

public sealed class AgentToolPolicyTests
{
    [Fact]
    public void Permissive_ExposesUnboundedDefaults()
    {
        var permissive = AgentToolPolicy.Permissive;

        permissive.MaxAttempts.Should().Be(int.MaxValue);
        permissive.MaxToolCalls.Should().Be(int.MaxValue);
        permissive.RequiredCapabilities.Should().BeEmpty();
    }

    [Fact]
    public void Permissive_IsSingleton()
    {
        AgentToolPolicy.Permissive.Should().BeSameAs(AgentToolPolicy.Permissive);
    }

    [Fact]
    public void Policy_RecordEquality_TreatsSameValuesAsEqual()
    {
        var capabilities = new[] { "files:read" };
        var a = new AgentToolPolicy(MaxAttempts: 3, MaxToolCalls: 5, RequiredCapabilities: capabilities);
        var b = new AgentToolPolicy(MaxAttempts: 3, MaxToolCalls: 5, RequiredCapabilities: capabilities);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Policy_DifferentMaxAttempts_NotEqual()
    {
        var a = new AgentToolPolicy(1, 1, []);
        var b = new AgentToolPolicy(2, 1, []);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Policy_With_ProducesUpdatedRecord()
    {
        var original = new AgentToolPolicy(1, 1, ["x"]);

        var updated = original with { MaxAttempts = 7 };

        updated.MaxAttempts.Should().Be(7);
        updated.MaxToolCalls.Should().Be(1);
        updated.RequiredCapabilities.Should().BeEquivalentTo(["x"]);
        original.MaxAttempts.Should().Be(1);
    }

    [Fact]
    public void Metadata_RecordEquality_TreatsSameValuesAsEqual()
    {
        var policy = new AgentToolPolicy(1, 1, []);

        var a = new AgentToolMetadata("read", policy);
        var b = new AgentToolMetadata("read", policy);

        a.Should().Be(b);
    }

    [Fact]
    public void Metadata_DifferentName_NotEqual()
    {
        var policy = new AgentToolPolicy(1, 1, []);

        var a = new AgentToolMetadata("read", policy);
        var b = new AgentToolMetadata("write", policy);

        a.Should().NotBe(b);
    }
}
