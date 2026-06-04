using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Context;

/// <summary>
///     <see cref="AIContextProvider"/> that conditionally attaches tool packs and
///     instructions per call. Each rule is evaluated against the inbound messages; when its
///     matcher returns <c>true</c>, the rule's tools are appended and its instruction fragment
///     concatenated onto the agent's effective instructions for that single invocation.
/// </summary>
/// <remarks>
///     <para>
///         This provider <em>injects</em> tools the agent never owned, scoped to the turn that
///         needed them. Pairs with <see cref="Governance.GovernedAIFunction"/> because injected
///         tools can be pre-wrapped before the rule's factory returns them.
///     </para>
///     <code>
///     var router = new QylConditionalToolProvider()
///         .Register("billing",   msgs => MentionsBilling(msgs),   () => billingTools,   "Use for billing.")
///         .Register("inventory", msgs => MentionsStock(msgs),     () => stockTools,     "Use for stock.");
///     options.WithQylAIContextProviders(router);
///     </code>
/// </remarks>
public sealed class QylConditionalToolProvider : AIContextProvider
{
    private readonly List<Rule> _rules = [];

    /// <summary>
    ///     Registers a conditional rule. <paramref name="matcher"/> is invoked with the current
    ///     inbound messages; when it returns <c>true</c>, <paramref name="toolFactory"/> is invoked
    ///     to produce the tools to attach and <paramref name="instructions"/> (if non-empty) is
    ///     appended to the per-call instructions.
    /// </summary>
    public QylConditionalToolProvider Register(
        string name,
        Func<IEnumerable<ChatMessage>, bool> matcher,
        Func<IList<AITool>> toolFactory,
        string? instructions = null)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(matcher);
        Guard.NotNull(toolFactory);

        _rules.Add(new Rule(matcher, toolFactory, instructions));
        return this;
    }

    /// <inheritdoc />
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(context);

        IEnumerable<ChatMessage> messages = context.AIContext.Messages ?? [];
        List<AITool> tools = [];
        var instructions = new StringBuilder();

        foreach (var rule in _rules.Where(rule => rule.Matcher(messages)))
        {
            tools.AddRange(rule.ToolFactory());
            if (!string.IsNullOrWhiteSpace(rule.Instructions))
            {
                if (instructions.Length > 0) instructions.AppendLine();
                instructions.Append(rule.Instructions);
            }
        }

        if (tools.Count is 0 && instructions.Length is 0)
            return ValueTask.FromResult(new AIContext());

        return ValueTask.FromResult(new AIContext
        {
            Tools = tools.Count > 0 ? tools : null,
            Instructions = instructions.Length > 0 ? instructions.ToString() : null,
        });
    }

    private sealed record Rule(
        Func<IEnumerable<ChatMessage>, bool> Matcher,
        Func<IList<AITool>> ToolFactory,
        string? Instructions);
}
