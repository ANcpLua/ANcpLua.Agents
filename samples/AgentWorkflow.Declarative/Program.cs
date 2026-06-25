// Showcase: build an executable MAF Workflow from an inline declarative (YAML) definition
// and surface it as a callable AIAgent — fully OFFLINE, no API keys, no live model.
//
// Combination: MAF Microsoft.Agents.AI.Workflows.Declarative (DeclarativeWorkflowBuilder /
//   DeclarativeWorkflowOptions / ResponseAgentProvider / Workflow)
//   x ANcpLua.Agents.Workflows.Declarative (QylDeclarativeAgent.Build / QylDeclarativeWorkflow.Build)
//   x ANcpLua.Agents.Workflows (Workflow.AsQylAIAgent, reached through QylDeclarativeAgent).
//
// Why no FakeChatClient here: DeclarativeWorkflowOptions does NOT accept an IChatClient. Its only
// model seam is a ResponseAgentProvider (an OpenAI-Responses-API-shaped contract). The brief's rule
// to build over FakeChatClient is conditional ("if the options require a chat client") — they do not.
// The YAML below uses only declarative control flow (SetVariable + ConditionGroup + SendActivity),
// so NO agent node is ever invoked. The single provider method the workflow root actually calls is
// CreateConversationAsync (to mint a conversation id); the remaining ResponseAgentProvider members are
// never reached, so they throw. The result is a real, end-to-end OFFLINE workflow execution.

using ANcpLua.Agents.Workflows.Declarative;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Declarative;
using Microsoft.Extensions.AI;

// A minimal declarative workflow. Schema mirrors the MAF declarative unit-test fixtures
// (Condition.yaml): a Workflow with an OnConversationStart trigger that sets a variable from the
// incoming message, branches on it with a ConditionGroup, then emits a final activity.
const string Yaml =
    """
    kind: Workflow
    trigger:
      kind: OnConversationStart
      id: classify_number
      actions:
        - kind: SetVariable
          id: read_input
          variable: Local.Value
          value: =Value(System.LastMessageText)

        - kind: ConditionGroup
          id: parity_branch
          conditions:
            - id: when_odd
              condition: =Mod(Local.Value, 2) = 1
              actions:
                - kind: SendActivity
                  id: say_odd
                  activity: The number is ODD.
            - id: when_even
              condition: =Mod(Local.Value, 2) = 0
              actions:
                - kind: SendActivity
                  id: say_even
                  activity: The number is EVEN.

        - kind: SendActivity
          id: say_done
          activity: Classification complete.
    """;

var options = new DeclarativeWorkflowOptions(new OfflineResponseAgentProvider());

// NOTE: the MAF DeclarativeWorkflowBuilder string overload treats its argument as a FILE PATH.
// To pass inline YAML, use the TextReader overloads (a StringReader). A TextReader is consumed
// once, so each Build call gets its own fresh reader.

// 1) Build the executable Workflow object and print its structure (offline, no execution).
Workflow workflow = QylDeclarativeWorkflow.Build(new StringReader(Yaml), options);
Console.WriteLine("== Declarative workflow structure ==");
Console.WriteLine($"start executor : {workflow.StartExecutorId}");
Console.WriteLine($"executors      : {workflow.ReflectExecutors().Count}");
Console.WriteLine($"edge sources   : {workflow.ReflectEdges().Count}");
Console.WriteLine();

// 2) Surface the SAME declarative YAML as a callable AIAgent and actually run it offline.
AIAgent agent = QylDeclarativeAgent.Build(
    new StringReader(Yaml),
    options,
    name: "number-classifier",
    description: "Classifies a number as odd or even via a declarative workflow.");

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("== Streaming run: input '7' ==");
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("7", session))
{
    if (!string.IsNullOrEmpty(update.Text))
        Console.WriteLine(update.Text);
}

Console.WriteLine();
Console.WriteLine("== Run: input '10' ==");
AgentResponse response = await agent.RunAsync("10", await agent.CreateSessionAsync());
foreach (ChatMessage message in response.Messages)
{
    if (!string.IsNullOrEmpty(message.Text))
        Console.WriteLine(message.Text);
}

// Minimal offline ResponseAgentProvider: mints a fake conversation id so the declarative root
// executor can start, and echoes added messages. No model, no network. The agent-invocation and
// message-retrieval members are never reached by this control-flow-only YAML, so they throw.
internal sealed class OfflineResponseAgentProvider : ResponseAgentProvider
{
    public override Task<string> CreateConversationAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Guid.NewGuid().ToString("N"));

    public override Task<ChatMessage> CreateMessageAsync(string conversationId, ChatMessage conversationMessage, CancellationToken cancellationToken = default)
        => Task.FromResult(conversationMessage);

    public override Task<ChatMessage> GetMessageAsync(string conversationId, string messageId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This offline provider does not retrieve stored messages.");

    public override IAsyncEnumerable<AgentResponseUpdate> InvokeAgentAsync(
        string agentId,
        string? agentVersion,
        string? conversationId,
        IEnumerable<ChatMessage>? messages,
        IDictionary<string, object?>? inputArguments,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This offline provider does not invoke model-backed agents.");

    public override IAsyncEnumerable<ChatMessage> GetMessagesAsync(
        string conversationId,
        int? limit = null,
        string? after = null,
        string? before = null,
        bool newestFirst = false,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This offline provider does not enumerate conversation history.");
}
