using ANcpLua.Agents.Governance;

namespace ANcpLua.Agents.Tests.Governance;

public sealed class GovernanceExceptionsTests
{
    [Fact]
    public void BudgetExceeded_DefaultCtor_HasMessage()
    {
        var ex = new AgentBudgetExceededException();

        ex.Message.Should().NotBeNullOrWhiteSpace();
        ex.ToolName.Should().BeNull();
        ex.BudgetKind.Should().BeNull();
        ex.Limit.Should().Be(0);
        ex.Attempted.Should().Be(0);
    }

    [Fact]
    public void BudgetExceeded_MessageCtor_PreservesMessage()
    {
        var ex = new AgentBudgetExceededException("custom");

        ex.Message.Should().Be("custom");
    }

    [Fact]
    public void BudgetExceeded_InnerExceptionCtor_PreservesInner()
    {
        var inner = new InvalidOperationException("root");

        var ex = new AgentBudgetExceededException("wrapper", inner);

        ex.Message.Should().Be("wrapper");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void BudgetExceeded_FullCtor_CarriesAllProperties()
    {
        var ex = new AgentBudgetExceededException("readFile", "MaxAttempts", limit: 3, attempted: 4);

        ex.ToolName.Should().Be("readFile");
        ex.BudgetKind.Should().Be("MaxAttempts");
        ex.Limit.Should().Be(3);
        ex.Attempted.Should().Be(4);
        ex.Message.Should().Contain("readFile").And.Contain("MaxAttempts").And.Contain("3").And.Contain("4");
    }

    [Fact]
    public void SpawnLimit_DefaultCtor_HasMessage()
    {
        var ex = new AgentSpawnLimitExceededException();

        ex.Message.Should().NotBeNullOrWhiteSpace();
        ex.RunId.Should().BeNull();
        ex.LimitKind.Should().BeNull();
        ex.RootRunId.Should().BeNull();
    }

    [Fact]
    public void SpawnLimit_MessageCtor_PreservesMessage()
    {
        var ex = new AgentSpawnLimitExceededException("custom");

        ex.Message.Should().Be("custom");
    }

    [Fact]
    public void SpawnLimit_InnerExceptionCtor_PreservesInner()
    {
        var inner = new InvalidOperationException("root");

        var ex = new AgentSpawnLimitExceededException("wrapper", inner);

        ex.Message.Should().Be("wrapper");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void SpawnLimit_FullCtor_CarriesAllProperties()
    {
        var ex = new AgentSpawnLimitExceededException("run-1", "depth", limit: 3, actual: 4, rootRunId: "root-1");

        ex.RunId.Should().Be("run-1");
        ex.LimitKind.Should().Be("depth");
        ex.Limit.Should().Be(3);
        ex.Actual.Should().Be(4);
        ex.RootRunId.Should().Be("root-1");
        ex.Message.Should().Contain("run-1").And.Contain("depth").And.Contain("root-1");
    }

    [Fact]
    public void CapabilityDenied_MessageCtor_PreservesMessage()
    {
        var ex = new AgentCapabilityDeniedException("denied");

        ex.Message.Should().Be("denied");
        ex.Capability.Should().BeNull();
        ex.ToolName.Should().BeNull();
        ex.GrantedCapabilities.Should().BeNull();
    }

    [Fact]
    public void Exceptions_AreInvalidOperationException()
    {
        new AgentBudgetExceededException().Should().BeAssignableTo<InvalidOperationException>();
        new AgentSpawnLimitExceededException().Should().BeAssignableTo<InvalidOperationException>();
        new AgentCapabilityDeniedException().Should().BeAssignableTo<InvalidOperationException>();
    }
}
