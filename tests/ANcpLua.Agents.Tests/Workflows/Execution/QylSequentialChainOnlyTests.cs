using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Tests.Workflows.Execution;

/// <summary>
/// Behavioral coverage for the MAF 1.13 <c>chainOnlyAgentResponses</c> sequential
/// orchestration mode surfaced through <see cref="QylAgentWorkflowExtensions"/>.
/// </summary>
public sealed class QylSequentialChainOnlyTests
{
    [Fact]
    public async Task AsQylSequentialAgent_ChainOnly_PassesOnlyPriorAgentResponseDownstream()
    {
        // Arrange
        using var writerClient = new FakeChatClient();
        writerClient.WithResponse("WRITER-DRAFT");
        using var editorClient = new FakeChatClient();
        editorClient.WithResponse("EDITED-FINAL");

        AIAgent writer = new ChatClientAgent(writerClient, name: "writer");
        AIAgent editor = new ChatClientAgent(editorClient, name: "editor");

        AIAgent pipeline = new[] { writer, editor }
            .AsQylSequentialAgent("chain-only-pipeline", chainOnlyAgentResponses: true);

        // Act
        AgentResponse response = await pipeline.RunAsync("USER-PROMPT");

        // Assert
        string editorInput = string.Join(
            "\n",
            editorClient.Calls.SelectMany(static call => call.Messages).Select(static message => message.Text));
        editorInput.Should().Contain("WRITER-DRAFT");
        editorInput.Should().NotContain("USER-PROMPT");
        response.Text.Should().Contain("EDITED-FINAL");
    }

    [Fact]
    public async Task AsQylSequentialAgent_Default_AccumulatesFullConversation()
    {
        // Arrange
        using var writerClient = new FakeChatClient();
        writerClient.WithResponse("WRITER-DRAFT");
        using var editorClient = new FakeChatClient();
        editorClient.WithResponse("EDITED-FINAL");

        AIAgent writer = new ChatClientAgent(writerClient, name: "writer");
        AIAgent editor = new ChatClientAgent(editorClient, name: "editor");

        AIAgent pipeline = new[] { writer, editor }.AsQylSequentialAgent("accumulating-pipeline");

        // Act
        await pipeline.RunAsync("USER-PROMPT");

        // Assert
        string editorInput = string.Join(
            "\n",
            editorClient.Calls.SelectMany(static call => call.Messages).Select(static message => message.Text));
        editorInput.Should().Contain("WRITER-DRAFT");
        editorInput.Should().Contain("USER-PROMPT");
    }

    [Fact]
    public void BuildQylSequential_ChainOnly_BuildsNamedWorkflow()
    {
        // Arrange
        using var chatClient = new FakeChatClient();
        AIAgent solo = new ChatClientAgent(chatClient, name: "solo");

        // Act
        var workflow = new[] { solo }.BuildQylSequential("named-chain-only", chainOnlyAgentResponses: true);

        // Assert
        workflow.Should().NotBeNull();
        workflow.Name.Should().Be("named-chain-only");
    }
}
