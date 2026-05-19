using ANcpLua.Agents.Mcp;

namespace ANcpLua.Agents.Tests.Hosting.Mcp;

public sealed class QylStdioMcpClientSmokeTests
{
    [Fact]
    public async Task CreateQylStdioMcpClientAsync_NonexistentCommand_Throws()
    {
        var act = async () => await QylMcpClientExtensions.CreateQylStdioMcpClientAsync(
            command: "this-command-does-not-exist-anywhere-on-PATH-12345",
            arguments: ["--help"],
            name: "smoke");

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateQylStdioMcpClientAsync_EmptyCommand_ThrowsArgumentException()
    {
        var act = () => QylMcpClientExtensions.CreateQylStdioMcpClientAsync(command: "");

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
