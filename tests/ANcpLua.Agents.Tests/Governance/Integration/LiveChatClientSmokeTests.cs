using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Governance.Integration;

public sealed class LiveChatClientSmokeTests
{
    [Fact]
    public async Task GetResponseAsync_AgainstLiveEndpoint_ReturnsContentAsync()
    {
        Assert.SkipUnless(IntegrationEnvironment.IsAvailable, IntegrationEnvironment.SkipReason);

        using var client = IntegrationEnvironment.CreateClient();
        using var cts = IntegrationEnvironment.CreateLinkedTimeoutSource(
            IntegrationEnvironment.SmokeTimeout, TestContext.Current.CancellationToken);

        var response = await client.GetResponseAsync(
            "Reply with the single word: pong.",
            cancellationToken: cts.Token);

        response.Should().NotBeNull();
        response.Text.Should().NotBeNullOrWhiteSpace();
    }
}
