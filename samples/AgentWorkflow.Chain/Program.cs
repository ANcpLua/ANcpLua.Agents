// Showcase: a sequential multi-step MAF Workflow assembled with the ANcpLua.Agents.Workflows
// Qyl facades, driven entirely offline by a FakeChatClient — no API keys required.
//
// Combination:
//   MAF Microsoft.Agents.AI.Workflows (WorkflowBuilder / ExecutorBinding / FunctionExecutor / Run)
//   x ANcpLua.Agents.Workflows (QylFunction, QylAgentExecutor, AddQylChain, RunQylAsync, ToQylMermaidString)
//   x ANcpLua.Agents.Testing (FakeChatClient) + ANcpLua.Agents (QylAgentOptionsBuilder).
//
// The chain has three stages that flow string -> string:
//   1. normalize : a pure QylFunction executor that cleans the raw ticket text.
//   2. triage    : a QylAgentExecutor wrapping a FakeChatClient-backed MAF agent.
//   3. format    : a pure QylFunction executor that renders the final report line.
// Each FunctionExecutor auto-sends its return value to the next stage (AutoSendMessageHandlerResultObject)
// and the final stage, registered via WithOutputFrom, auto-yields its result as a WorkflowOutputEvent
// (AutoYieldOutputHandlerResultObject) — both defaults on FunctionExecutor.

using ANcpLua.Agents.Facades;
using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

// --- Offline agent for the middle stage: seed the FakeChatClient with a canned triage answer. ---
using var chatClient = new FakeChatClient();
chatClient.WithResponse("priority=HIGH; team=billing; reason=payment failure reported");

AIAgent triageAgent = new QylAgentOptionsBuilder()
    .WithName("triage-agent")
    .WithDescription("Classifies a support ticket into priority/team/reason.")
    .WithInstructions("Classify the support ticket. Reply as priority=...; team=...; reason=...")
    .BuildAgent(chatClient);

// --- Stage 1: pure function executor — normalize the raw ticket text. ---
FunctionExecutor<string, string> normalize =
    QylExecutorFactoryExtensions.QylFunction<string, string>(
        "normalize",
        static raw => raw.Trim().ReplaceLineEndings(" "));

// --- Stage 2: agent executor — run the offline FakeChatClient agent over the normalized text. ---
FunctionExecutor<string, string> triage =
    QylExecutorFactoryExtensions.QylAgentExecutor<string>(
        "triage",
        triageAgent,
        static normalized => $"Triage this ticket: {normalized}");

// --- Stage 3: pure function executor — render the final report line. ---
FunctionExecutor<string, string> format =
    QylExecutorFactoryExtensions.QylFunction<string, string>(
        "format",
        static classification => $"[TICKET REPORT] {classification}");

// Materialize the bindings once so the chain edges and output registration share identity.
ExecutorBinding source = normalize;
ExecutorBinding triageStage = triage;
ExecutorBinding formatStage = format;

// --- Compose the linear chain: normalize -> triage -> format, output from the last stage. ---
Workflow workflow = new WorkflowBuilder(source)
    .AddQylChain(source, [triageStage, formatStage])
    .WithOutputFrom(formatStage)
    .Build();

Console.WriteLine("Workflow graph (Mermaid):");
Console.WriteLine(workflow.ToQylMermaidString());
Console.WriteLine();

// --- Run the chain once and print the terminal output. ---
Run run = await workflow.RunQylAsync("  Customer: my payment failed twice today!  ");

string finalOutput = run.OutgoingEvents
    .OfType<WorkflowOutputEvent>()
    .Select(static evt => evt.As<string>())
    .LastOrDefault(static text => text is not null) ?? "(no output produced)";

Console.WriteLine($"Final chain output: {finalOutput}");
