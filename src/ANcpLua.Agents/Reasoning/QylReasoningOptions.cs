namespace ANcpLua.Agents.Reasoning;

/// <summary>
///     Provider-agnostic reasoning knobs. Each hosting facade (OpenAI, Anthropic, Gemini)
///     translates these to the provider's native shape (<c>CreateResponseOptions.ReasoningOptions</c>,
///     <c>ThinkingConfig</c>, <c>GenerateContentConfig.ThinkingConfig</c>) via a
///     <c>WithQylReasoning</c> extension on its own surface.
/// </summary>
/// <param name="Effort">Coarse reasoning intensity dial. <c>null</c> = provider default.</param>
/// <param name="BudgetTokens">Cap on tokens spent on reasoning. <c>null</c> = provider default.</param>
/// <param name="Summary">When <c>true</c>, request a textual reasoning summary alongside the answer.</param>
public sealed record QylReasoningOptions(
    QylReasoningEffort? Effort = null,
    int? BudgetTokens = null,
    bool? Summary = null);

/// <summary>Coarse reasoning intensity levels mapped per-provider by hosting facades.</summary>
public enum QylReasoningEffort
{
    /// <summary>Minimal reasoning; fastest response.</summary>
    Minimal,
    /// <summary>Low reasoning effort.</summary>
    Low,
    /// <summary>Medium reasoning effort.</summary>
    Medium,
    /// <summary>High reasoning effort; longest deliberation.</summary>
    High,
}
