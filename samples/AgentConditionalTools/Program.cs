using System.ComponentModel;
using ANcpLua.Agents.Context;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Showcase: conditional tool exposure — tools that exist only on the turns that need them.
//
// Combination:
//   MAF ChatClientAgent + AIContextProvider/ChatClientAgentOptions
//   x ANcpLua.Agents (QylContextExtensions.WithQylConditionalTools / QylConditionalToolProvider.Register)
//   x ANcpLua.Agents.Testing (FakeChatClient, fully offline — no API keys).
//
// How it works: QylConditionalToolProvider is an AIContextProvider. Per invocation it inspects the
// inbound messages; when a rule's predicate matches, that rule's tools are appended to the AIContext.
// MAF's ChatClientAgent materializes AIContext.Tools into the ChatOptions it sends to the IChatClient.
// Because FakeChatClient records every call's ChatOptions, we can read back exactly which tools were
// active on each turn and prove they appear only for the matching prompt.

using var chatClient = new FakeChatClient();
chatClient
    .WithResponse("I have queued a refund for order A-1001.")
    .WithResponse("Vienna is the capital of Austria.");

// Billing tool pack — only relevant when the user is talking about money.
AITool[] billingTools =
[
    AIFunctionFactory.Create(RefundOrderAsync, new AIFunctionFactoryOptions { Name = "refund_order" }),
    AIFunctionFactory.Create(LookupInvoiceAsync, new AIFunctionFactoryOptions { Name = "lookup_invoice" }),
];

var options = new ChatClientAgentOptions
{
    Name = "support-agent",
    ChatOptions = new ChatOptions { Instructions = "You are a customer support agent." },
};

// Register a single conditional rule: expose the billing tools only when the conversation
// mentions billing/refund/invoice. Every other turn the agent has zero tools attached.
options.WithQylConditionalTools(router => router.Register(
    name: "billing",
    matcher: messages => messages.Any(MentionsBilling),
    toolFactory: () => billingTools,
    instructions: "Use the billing tools to resolve refund and invoice questions."));

var agent = new ChatClientAgent(chatClient, options);

Console.WriteLine("=== Conditional tool exposure (offline) ===\n");

await RunAndReportAsync(agent, chatClient, "I'd like a refund for order A-1001, please.");
await RunAndReportAsync(agent, chatClient, "What is the capital of Austria?");

return;

static async Task RunAndReportAsync(ChatClientAgent agent, FakeChatClient chatClient, string prompt)
{
    AgentSession session = await agent.CreateSessionAsync();
    AgentResponse response = await agent.RunAsync(prompt, session);

    // The tools the provider attached for this turn land in the ChatOptions the agent sent
    // to the chat client — FakeChatClient captured them in LastOptions.
    var activeTools = chatClient.LastOptions?.Tools;
    var toolNames = activeTools is { Count: > 0 }
        ? string.Join(", ", activeTools.Select(static tool => tool.Name))
        : "(none)";

    Console.WriteLine($"Prompt        : {prompt}");
    Console.WriteLine($"Active tools  : {toolNames}");
    Console.WriteLine($"Agent reply   : {response.Text}");
    Console.WriteLine();
}

static bool MentionsBilling(ChatMessage message)
{
    var text = message.Text;
    return text.Contains("refund", StringComparison.OrdinalIgnoreCase)
           || text.Contains("invoice", StringComparison.OrdinalIgnoreCase)
           || text.Contains("billing", StringComparison.OrdinalIgnoreCase);
}

static Task<string> RefundOrderAsync(
    [Description("Order id to refund.")] string orderId,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"refund:{orderId}:queued");
}

static Task<string> LookupInvoiceAsync(
    [Description("Invoice number to look up.")] string invoiceNumber,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"invoice:{invoiceNumber}:paid");
}
