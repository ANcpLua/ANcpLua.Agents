using Microsoft.CodeAnalysis;

namespace Qyl.DurableAgents.Generators.Analyzers;

internal static class QylDiagnosticDescriptors
{
    private const string Category = "Qyl.DurableAgents";

    public static readonly DiagnosticDescriptor OrchestratorMustBeStatic = new(
        id: "QYL001",
        title: "[QylOrchestrator] method must be static",
        messageFormat: "Method '{0}' is marked [QylOrchestrator] but is not static or has unsupported accessibility — declare it 'public static' or 'internal static'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OrchestratorFirstParamMustBeContext = new(
        id: "QYL002",
        title: "[QylOrchestrator] first parameter must be TaskOrchestrationContext",
        messageFormat: "Method '{0}' is marked [QylOrchestrator] but its first parameter is '{1}', not Microsoft.DurableTask.TaskOrchestrationContext",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ActivityMustBeStatic = new(
        id: "QYL003",
        title: "[QylActivity] method must be static",
        messageFormat: "Method '{0}' is marked [QylActivity] but is not static or has unsupported accessibility — declare it 'public static' or 'internal static'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ActivityTooManyParameters = new(
        id: "QYL004",
        title: "[QylActivity] method has too many parameters",
        messageFormat: "Method '{0}' is marked [QylActivity] with {1} parameters — supported shapes are: '()', '(TInput)', '(TaskActivityContext)', '(TaskActivityContext, TInput)', or '(TInput, TaskActivityContext)'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AgentEndpointPatternEmpty = new(
        id: "QYL005",
        title: "[QylAgentEndpoint] pattern must be non-empty",
        messageFormat: "Method '{0}' is marked [QylAgentEndpoint] with an empty pattern — supply a 'VERB /route' string such as \"POST /reports\"",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AgentEndpointUnsupportedVerb = new(
        id: "QYL006",
        title: "[QylAgentEndpoint] HTTP verb is not supported",
        messageFormat: "[QylAgentEndpoint] verb '{0}' is not supported — use one of GET, POST, PUT, DELETE, PATCH",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
