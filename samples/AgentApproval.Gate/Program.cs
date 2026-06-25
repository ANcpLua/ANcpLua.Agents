using System.ComponentModel;
using ANcpLua.Agents.Governance;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Showcase: human-in-the-loop tool approval. The agent is built with a single-call
// approval gate (ANcpLua.Agents.Governance.QylAgentGovernanceExtensions.UseQylApproval)
// that runs a predicate before every tool invocation. A denied call throws
// AgentApprovalDeniedException; an approved call proceeds to the real tool.
//
// The gate runs as function-invocation middleware below the FunctionInvokingChatClient (FIC).
// FIC normally captures a tool exception and feeds it back to the model (up to
// MaximumConsecutiveErrorsPerRequest = 3 by default), which would swallow a denial. Setting that
// limit to 0 makes FIC rethrow immediately, so the denial surfaces to the caller as designed.
//
// Combination: MAF ChatClientAgent function-invocation middleware
//   x ANcpLua.Agents.Governance (UseQylApproval / AgentApprovalDeniedException)
//   x ANcpLua.Agents.Testing (FakeChatClient seeded with a tool call) — fully offline.

var refundTool = AIFunctionFactory.Create(IssueRefundAsync);
var runOptions = new ChatClientAgentRunOptions(new ChatOptions { Tools = [refundTool] });

// --- Run 1: approval GRANTED -> the tool runs and the agent returns its answer. -----------
{
    using var chatClient = SeedRefundFunctionInvoker();
    var agent = BuildGatedAgent(chatClient, approve: true);

    AgentSession session = await agent.CreateSessionAsync();
    AgentResponse response = await agent.RunAsync("Refund order ORD-42.", session, runOptions);

    Console.WriteLine($"[granted] {response.Text}");
}

// --- Run 2: approval DENIED -> the gate throws before the tool can run. --------------------
{
    using var chatClient = SeedRefundFunctionInvoker();
    var agent = BuildGatedAgent(chatClient, approve: false);

    AgentSession session = await agent.CreateSessionAsync();
    try
    {
        var r = await agent.RunAsync("Refund order ORD-42.", session, runOptions);
        Console.WriteLine($"[denied] unexpected: tool was allowed to run. text='{r.Text}'");
        foreach (var m in r.Messages)
        foreach (var c in m.Contents)
            Console.WriteLine($"    content={c.GetType().Name} {(c is Microsoft.Extensions.AI.FunctionResultContent frc ? "ex=" + frc.Exception?.GetType().Name : "")}");
    }
    catch (AgentApprovalDeniedException ex)
    {
        Console.WriteLine($"[denied] {ex.Message} (tool: {ex.ToolName})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[denied] OTHER EXCEPTION {ex.GetType().FullName}: {ex.Message}");
    }
}

// Seeds an offline FakeChatClient (request the issue_refund tool, then a final answer).
static FakeChatClient SeedRefundFunctionInvoker()
{
    var chatClient = new FakeChatClient();
    chatClient
        .WithResponse(
            [new FunctionCallContent("call_1", "issue_refund", new Dictionary<string, object?> { ["orderId"] = "ORD-42" })],
            ChatFinishReason.ToolCalls)
        .WithResponse("Refund issued for order ORD-42.");

    return chatClient;
}

// Builds a ChatClientAgent over the seeded client, then layers the approval gate on top via the fluent builder.
static AIAgent BuildGatedAgent(IChatClient chatClient, bool approve) =>
    new ChatClientAgent(
            chatClient,
            name: "refund-agent")
        .AsBuilder()
        .UseQylApproval((_, context) =>
        {
            Console.WriteLine($"  approval requested for tool '{context.Function.Name}' -> {(approve ? "GRANTED" : "DENIED")}");
            return ValueTask.FromResult(approve);
        })
        .Build();

static Task<string> IssueRefundAsync(
    [Description("The order id to refund.")] string orderId,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"refund:{orderId}:ok");
}
