using ANcpLua.Agents.Factory;

namespace ANcpLua.Agents.Tests.Factory;

public sealed class AgentChatClientFactoryTests
{
    [Fact]
    public void TryCreate_NullApiKey_ReturnsNull() =>
        AgentChatClientFactory.TryCreate(new AgentChatClientOptions(ApiKey: null)).Should().BeNull();

    [Fact]
    public void TryCreate_EmptyApiKey_ReturnsNull() =>
        AgentChatClientFactory.TryCreate(new AgentChatClientOptions(ApiKey: "")).Should().BeNull();

    [Fact]
    public void TryCreate_WithApiKey_ReturnsClient()
    {
        var client = AgentChatClientFactory.TryCreate(new AgentChatClientOptions(ApiKey: "sk-test-not-real"));
        client.Should().NotBeNull();
    }

    [Fact]
    public void TryCreate_WithEndpoint_ReturnsClient()
    {
        var client = AgentChatClientFactory.TryCreate(
            new AgentChatClientOptions(ApiKey: "sk-test", Model: "llama3", Endpoint: "http://localhost:11434/v1"));
        client.Should().NotBeNull();
    }
}
