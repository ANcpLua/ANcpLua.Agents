using System.ComponentModel;
using ANcpLua.Agents.Governance;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Showcase: governed agent tools over an offline FakeChatClient — no API keys required.
//
// Combination:
//   MAF (chat-client-agent function-invoking loop + AIAgentBuilder.Use middleware)
//   x ANcpLua.Agents.Governance (QylToolSet.From<T>, AgentToolPolicy, AgentBudgetEnforcer,
//                                AgentConcurrencyLimiter, AgentCapabilityContext,
//                                AIAgentBuilder.UseQylGovernance)
//   x ANcpLua.Agents.Testing (FakeChatClient scripted tool-call loop).
//
// Two enforcement paths are demonstrated, both with the agent continuing after a denied tool:
//   PART 1 — QylToolSet.From<T> wraps a host type's methods in GovernedAIFunction. A
//            required-capability policy denies one tool; a MaxAttempts policy trips on the
//            second call. The MAF loop surfaces each governance failure as a
//            FunctionResultContent carrying the thrown governance exception.
//   PART 2 — AIAgentBuilder.UseQylGovernance(...) inserts the same budget + concurrency +
//            capability checks as run-level middleware around every tool invocation.

// ---------------------------------------------------------------------------------------------
// Shared governance primitives. "billing:read" is granted; "billing:write" is intentionally NOT.
// ---------------------------------------------------------------------------------------------
var budget = new AgentBudgetEnforcer();
using var concurrency = new AgentConcurrencyLimiter(defaultLimit: 2);
var capabilities = new AgentCapabilityContext(grantedCapabilities: ["billing:read"]);

// =============================================================================================
// PART 1 — QylToolSet.From<T>: project a host type's methods into governed tools.
// =============================================================================================
Console.WriteLine("== PART 1: QylToolSet.From<T> (per-tool GovernedAIFunction) ==");

// A uniform policy that requires the (ungranted) "billing:write" capability and caps attempts.
var refundPolicy = new AgentToolPolicy(
    MaxAttempts: 1,
    MaxToolCalls: 2,
    RequiredCapabilities: ["billing:write"]);

IList<AITool> refundTools = QylToolSet.From(
    new BillingTools(),
    policy: refundPolicy,
    budget: budget,
    concurrency: concurrency,
    capabilities: capabilities);

// The model "tries" to call IssueRefund. The capability is missing, so the GovernedAIFunction
// throws AgentCapabilityDeniedException before the body runs; the MAF loop feeds that back as a
// FunctionResultContent and lets the model produce a final answer.
using var refundClient = new FakeChatClient();
refundClient
    .WithResponse(
        contents: [new FunctionCallContent("call-refund-1", "IssueRefund",
            new Dictionary<string, object?> { ["orderId"] = "A-100", ["amount"] = 42.0 })],
        finishReason: ChatFinishReason.ToolCalls)
    .WithResponse("I could not issue the refund: the billing:write capability is not granted.");

var refundAgent = QylAgentFactory.Create(
    refundClient,
    options => options
        .WithName("refund-agent")
        .WithInstructions("Issue refunds when asked, using the IssueRefund tool.")
        .WithTools([.. refundTools]));

AgentSession refundSession = await refundAgent.CreateSessionAsync();
AgentResponse refundResponse = await refundAgent.RunAsync("Refund order A-100 for $42.", refundSession);

Console.WriteLine($"  agent reply : {refundResponse.Text}");
Console.WriteLine($"  enforced    : {DescribeToolFailures(refundClient)}");
Console.WriteLine($"  body ran    : {BillingTools.RefundsIssued} refund(s) (expected 0 — capability denied)");

// =============================================================================================
// PART 2 — UseQylGovernance middleware with a policyResolver enforcing MaxAttempts.
// =============================================================================================
Console.WriteLine();
Console.WriteLine("== PART 2: AIAgentBuilder.UseQylGovernance (run-level middleware) ==");

// "lookup_invoice" only needs the granted "billing:read" capability, but is capped to a single
// attempt. The model calls it twice; the second reservation trips AgentBudgetExceededException.
var lookupPolicy = new AgentToolPolicy(
    MaxAttempts: 1,
    MaxToolCalls: 5,
    RequiredCapabilities: ["billing:read"]);

var lookupTool = AIFunctionFactory.Create(
    InvoiceTools.LookupInvoice,
    new AIFunctionFactoryOptions { Name = "lookup_invoice" });

using var lookupClient = new FakeChatClient();
lookupClient
    .WithResponse(
        contents: [new FunctionCallContent("call-lookup-1", "lookup_invoice",
            new Dictionary<string, object?> { ["invoiceId"] = "INV-1" })],
        finishReason: ChatFinishReason.ToolCalls)
    .WithResponse(
        contents: [new FunctionCallContent("call-lookup-2", "lookup_invoice",
            new Dictionary<string, object?> { ["invoiceId"] = "INV-2" })],
        finishReason: ChatFinishReason.ToolCalls)
    .WithResponse("Looked up the first invoice; the second lookup was over budget.");

// The tools are registered ungoverned; UseQylGovernance supplies enforcement at the agent layer.
AIAgent lookupAgent = QylAgentFactory.Create(
    lookupClient,
    options => options
        .WithName("lookup-agent")
        .WithInstructions("Look up invoices with the lookup_invoice tool.")
        .WithTools([lookupTool]),
    pipeline => pipeline.UseQylGovernance(
        capabilities,
        budget,
        concurrency,
        policyResolver: name => name == "lookup_invoice" ? lookupPolicy : AgentToolPolicy.Permissive));

AgentResponse lookupResponse = await lookupAgent.RunAsync("Look up invoices INV-1 and INV-2.");

Console.WriteLine($"  agent reply : {lookupResponse.Text}");
Console.WriteLine($"  enforced    : {DescribeToolFailures(lookupClient)}");
Console.WriteLine($"  body ran    : {InvoiceTools.Lookups} lookup(s) (expected 1 — MaxAttempts=1)");

return;

// ---------------------------------------------------------------------------------------------
// Helpers and tools.
// ---------------------------------------------------------------------------------------------

// Inspects what the FakeChatClient received: the MAF loop reports governance denials as
// FunctionResultContent whose Exception is the thrown governance exception. The final recorded
// call carries the full cumulative session history, so it holds every governance failure once.
static string DescribeToolFailures(FakeChatClient client)
{
    if (client.Calls.Count == 0)
        return "no tool failures";

    var failures = client.Calls[^1].Messages
        .SelectMany(static message => message.Contents)
        .OfType<FunctionResultContent>()
        .Where(static result => result.Exception is not null)
        .Select(static result => result.Exception!.GetType().Name)
        .ToList();

    return failures.Count == 0 ? "no tool failures" : string.Join(", ", failures);
}

// Invoice lookup tool plus its invocation counter (kept in a type because top-level statements
// cannot declare fields).
internal static class InvoiceTools
{
    private static int s_lookups;

    public static int Lookups => Volatile.Read(ref s_lookups);

    public static string LookupInvoice([Description("Invoice id.")] string invoiceId)
    {
        Interlocked.Increment(ref s_lookups);
        return $"{invoiceId}: paid";
    }
}

// Host type whose public instance methods QylToolSet.From<T> projects into governed tools.
internal sealed class BillingTools
{
    private static int s_refundsIssued;

    public static int RefundsIssued => Volatile.Read(ref s_refundsIssued);

    [Description("Issue a refund for an order. Requires the billing:write capability.")]
    public string IssueRefund(
        [Description("Order id.")] string orderId,
        [Description("Refund amount.")] double amount)
    {
        Interlocked.Increment(ref s_refundsIssued);
        return $"refunded {amount:C} for {orderId}";
    }
}
