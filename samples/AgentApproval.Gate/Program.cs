using System.ComponentModel;
using ANcpLua.Agents.Governance;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Showcase: two ways to gate a side-effecting tool, side by side, and WHEN to use each.
//
//   Path A — DETERMINISTIC GATE (ANcpLua.Agents.Governance.UseQylApproval).
//     A predicate runs before every tool call. Denial throws AgentApprovalDeniedException
//     straight to the caller; the tool never runs. Single-call, no extra round trip, no
//     conversation state. Use this for synchronous policy you can decide in-process right now
//     (feature flag, RBAC claim, kill switch) where a denied call should fail the run.
//
//   Path B — NATIVE HUMAN-IN-THE-LOOP (MEAI ApprovalRequiredAIFunction + the MAF
//     ToolApprovalRequestContent protocol). The agent PAUSES on the tool call and hands a
//     ToolApprovalRequestContent back to the caller; a human decides out of band, then the
//     caller resumes the same session with a ToolApprovalResponseContent. Multi-turn. Use this
//     when approval needs a person, an external system, or time the request can't block on.
//
// Both run fully offline against a seeded FakeChatClient — no API key, no network.

// ---------------------------------------------------------------------------------------------
// Path A: deterministic gate — UseQylApproval throws on denial.
// ---------------------------------------------------------------------------------------------
//
// UseQylApproval is function-invocation middleware: it runs INSIDE the FunctionInvokingChatClient
// (FICC) loop. By default FICC captures a tool exception, records it as a tool-result error, and
// feeds it back to the model (up to MaximumConsecutiveErrorsPerRequest = 3) — which would swallow a
// denial. Pre-building the FICC with MaximumConsecutiveErrorsPerRequest = 0 makes it rethrow
// immediately, so AgentApprovalDeniedException surfaces to the caller as designed. The inner agent
// reuses a FICC already present on the chat client instead of inserting its own
// (ChatClientExtensions: `if (chatClient.GetService<FunctionInvokingChatClient>() is null)`).

var deterministicTool = AIFunctionFactory.Create(IssueRefundAsync, "issue_refund");
var deterministicOptions = new ChatClientAgentRunOptions(new ChatOptions { Tools = [deterministicTool] });

// --- A1: approval GRANTED -> the tool runs and the agent returns its answer. ------------------
{
    using var chatClient = SeedRefundClient();
    var agent = BuildGatedAgent(chatClient, approve: true);

    AgentSession session = await agent.CreateSessionAsync();
    AgentResponse response = await agent.RunAsync("Refund order ORD-42.", session, deterministicOptions);

    Console.WriteLine($"[A granted] {response.Text}");
}

// --- A2: approval DENIED -> the gate throws before the tool can run. --------------------------
{
    using var chatClient = SeedRefundClient();
    var agent = BuildGatedAgent(chatClient, approve: false);

    AgentSession session = await agent.CreateSessionAsync();
    try
    {
        await agent.RunAsync("Refund order ORD-42.", session, deterministicOptions);
        Console.WriteLine("[A denied] unexpected: the tool was allowed to run.");
    }
    catch (AgentApprovalDeniedException ex)
    {
        Console.WriteLine($"[A denied] {ex.Message} (tool: {ex.ToolName})");
    }
}

// ---------------------------------------------------------------------------------------------
// Path B: native human-in-the-loop — ApprovalRequiredAIFunction + ToolApprovalRequestContent.
// ---------------------------------------------------------------------------------------------
//
// Wrapping the tool in ApprovalRequiredAIFunction tells FICC to PAUSE instead of invoking: the run
// returns a ToolApprovalRequestContent rather than a result. The caller resumes the SAME session
// (with the same tools in options) by sending each request's CreateResponse(...) back as a user
// message. This is the entire ceremony QylApprovalGate.RequireQylApproval wraps — it just returns
// `new ApprovalRequiredAIFunction(function)`, so the raw type is shown here.
{
    var hitlTool = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(IssueRefundAsync, "issue_refund"));
    var hitlOptions = new ChatClientAgentRunOptions(new ChatOptions { Tools = [hitlTool] });

    using var chatClient = SeedRefundClient();
    var agent = QylAgentFactory.Create(
        chatClient,
        static options => options.WithName("refund-agent"));
    AgentSession session = await agent.CreateSessionAsync();

    // Turn 1: the model asks for the tool; the agent pauses and surfaces an approval request.
    AgentResponse turn1 = await agent.RunAsync("Refund order ORD-42.", session, hitlOptions);
    var requests = turn1.Messages
        .SelectMany(static m => m.Contents)
        .OfType<ToolApprovalRequestContent>()
        .ToList();

    foreach (var request in requests)
    {
        var toolName = (request.ToolCall as FunctionCallContent)?.Name ?? request.ToolCall.CallId;
        Console.WriteLine($"[B paused] approval requested for tool '{toolName}' (request {request.RequestId})");
    }

    // A human decides out of band. Resume the session with the approval responses.
    var approvals = requests
        .Select(static request => new ChatMessage(
            ChatRole.User,
            [request.CreateResponse(approved: true, reason: "Approved by reviewer.")]))
        .ToList();

    // Turn 2: the agent resumes, runs the now-approved tool, and answers.
    AgentResponse turn2 = await agent.RunAsync(approvals, session, hitlOptions);
    Console.WriteLine($"[B resumed] {turn2.Text}");
}

// Seeds an offline FakeChatClient: request the issue_refund tool, then deliver a final answer.
static FakeChatClient SeedRefundClient()
{
    var chatClient = new FakeChatClient();
    chatClient
        .WithResponse(
            [new FunctionCallContent("call_1", "issue_refund", new Dictionary<string, object?> { ["orderId"] = "ORD-42" })],
            ChatFinishReason.ToolCalls)
        .WithResponse("Refund issued for order ORD-42.");

    return chatClient;
}

// The Qyl factory builds the inner chat-client agent with a FICC that rethrows on denial,
// layers the deterministic approval gate inside mandatory telemetry, and returns only the wrapper.
static AIAgent BuildGatedAgent(IChatClient chatClient, bool approve) =>
    QylAgentFactory.Create(
        chatClient
            .AsBuilder()
            .UseFunctionInvocation(configure: static ficc => ficc.MaximumConsecutiveErrorsPerRequest = 0)
            .Build(),
        static options => options.WithName("refund-agent"),
        pipeline => pipeline.UseQylApproval((_, context) =>
        {
            Console.WriteLine($"  approval requested for tool '{context.Function.Name}' -> {(approve ? "GRANTED" : "DENIED")}");
            return ValueTask.FromResult(approve);
        }));

static Task<string> IssueRefundAsync(
    [Description("The order id to refund.")] string orderId,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"refund:{orderId}:ok");
}
