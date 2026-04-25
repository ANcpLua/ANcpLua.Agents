using ANcpLua.Agents.Governance;

namespace ANcpLua.Agents.Tests.Governance;

public sealed class AgentCapabilityContextTests
{
    [Fact]
    public void Verify_AllGranted_DoesNotThrow()
    {
        var ctx = new AgentCapabilityContext(["files:read", "files:write"]);

        var act = () => ctx.Verify(["files:read"]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Verify_MissingCapability_Throws()
    {
        var ctx = new AgentCapabilityContext(["files:read"]);

        var act = () => ctx.Verify(["files:read", "secrets:read"]);

        act.Should().Throw<AgentCapabilityDeniedException>()
            .WithMessage("*secrets:read*");
    }

    [Fact]
    public void Verify_EmptyRequiredList_DoesNotThrow()
    {
        var ctx = new AgentCapabilityContext();

        var act = () => ctx.Verify([]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Grant_AddsCapability_HasCapabilityReturnsTrue()
    {
        var ctx = new AgentCapabilityContext();

        ctx.HasCapability("net:fetch").Should().BeFalse();

        ctx.Grant("net:fetch");

        ctx.HasCapability("net:fetch").Should().BeTrue();
    }

    [Fact]
    public void Revoke_RemovesCapability_VerifyThrowsAfterward()
    {
        var ctx = new AgentCapabilityContext(["net:fetch"]);

        ctx.HasCapability("net:fetch").Should().BeTrue();

        ctx.Revoke("net:fetch");

        ctx.HasCapability("net:fetch").Should().BeFalse();
        var act = () => ctx.Verify(["net:fetch"]);
        act.Should().Throw<AgentCapabilityDeniedException>();
    }

    [Fact]
    public void Grant_IsIdempotent()
    {
        var ctx = new AgentCapabilityContext();

        ctx.Grant("x");
        ctx.Grant("x");

        ctx.HasCapability("x").Should().BeTrue();
    }

    [Fact]
    public void Revoke_OfMissingCapability_DoesNotThrow()
    {
        var ctx = new AgentCapabilityContext();

        var act = () => ctx.Revoke("never-granted");

        act.Should().NotThrow();
    }

    [Fact]
    public void Capabilities_AreCaseSensitive()
    {
        var ctx = new AgentCapabilityContext(["Files:Read"]);

        ctx.HasCapability("files:read").Should().BeFalse();
        ctx.HasCapability("Files:Read").Should().BeTrue();
    }

    [Fact]
    public void DeniedException_FullCtor_CarriesProperties()
    {
        var ex = new AgentCapabilityDeniedException("secrets:read", "loadSecret", ["files:read"]);

        ex.Capability.Should().Be("secrets:read");
        ex.ToolName.Should().Be("loadSecret");
        ex.GrantedCapabilities.Should().BeEquivalentTo(["files:read"]);
        ex.Message.Should().Contain("secrets:read").And.Contain("loadSecret");
    }

    [Fact]
    public void DeniedException_DefaultCtor_HasMessage()
    {
        var ex = new AgentCapabilityDeniedException();

        ex.Message.Should().NotBeNullOrWhiteSpace();
        ex.Capability.Should().BeNull();
    }

    [Fact]
    public void DeniedException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("root cause");

        var ex = new AgentCapabilityDeniedException("wrapper", inner);

        ex.Message.Should().Be("wrapper");
        ex.InnerException.Should().BeSameAs(inner);
    }
}
