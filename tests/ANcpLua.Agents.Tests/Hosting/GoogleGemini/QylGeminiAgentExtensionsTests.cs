using ANcpLua.Agents.Hosting.GoogleGemini;
using Google.GenAI;

namespace ANcpLua.Agents.Tests.Hosting.GoogleGemini;

public sealed class QylGeminiAgentExtensionsTests
{
    [Fact]
    public void AsQylGeminiAgent_NullClient_Throws()
    {
        Client client = null!;

        var act = () => client.AsQylGeminiAgent("gemini-3-flash-preview");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AsQylGeminiAgent_BlankModel_Throws(string model)
    {
        using Client client = new(apiKey: "test-key");

        var act = () => client.AsQylGeminiAgent(model);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AsQylGeminiAgent_NullOptions_Throws()
    {
        using Client client = new(apiKey: "test-key");

        var act = () => client.AsQylGeminiAgent("gemini-3-flash-preview", options: null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
