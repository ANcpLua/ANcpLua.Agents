using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Qyl.DurableAgents.Generators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class QylDurableAgentsDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableArray<string> s_supportedVerbs =
        ["GET", "POST", "PUT", "DELETE", "PATCH"];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        QylDiagnosticDescriptors.OrchestratorMustBeStatic,
        QylDiagnosticDescriptors.OrchestratorFirstParamMustBeContext,
        QylDiagnosticDescriptors.ActivityMustBeStatic,
        QylDiagnosticDescriptors.ActivityTooManyParameters,
        QylDiagnosticDescriptors.AgentEndpointPatternEmpty,
        QylDiagnosticDescriptors.AgentEndpointUnsupportedVerb,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static start =>
        {
            var orchestrator = start.Compilation.GetTypeByMetadataName(
                GeneratorPipelineHelpers.QylOrchestratorAttributeMetadataName);
            var activity = start.Compilation.GetTypeByMetadataName(
                GeneratorPipelineHelpers.QylActivityAttributeMetadataName);
            var endpoint = start.Compilation.GetTypeByMetadataName(
                GeneratorPipelineHelpers.QylAgentEndpointAttributeMetadataName);

            if (orchestrator is null && activity is null && endpoint is null) return;

            var orchestrationContext = start.Compilation.GetTypeByMetadataName(
                GeneratorPipelineHelpers.TaskOrchestrationContextMetadataName);
            var activityContext = start.Compilation.GetTypeByMetadataName(
                GeneratorPipelineHelpers.TaskActivityContextMetadataName);

            start.RegisterSymbolAction(
                ctx => Analyze(ctx, orchestrator, activity, endpoint, orchestrationContext, activityContext),
                SymbolKind.Method);
        });
    }

    private static void Analyze(
        SymbolAnalysisContext context,
        INamedTypeSymbol? orchestratorAttr,
        INamedTypeSymbol? activityAttr,
        INamedTypeSymbol? endpointAttr,
        INamedTypeSymbol? orchestrationContextType,
        INamedTypeSymbol? activityContextType)
    {
        var method = (IMethodSymbol)context.Symbol;
        foreach (var attr in method.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null) continue;

            if (orchestratorAttr is not null && SymbolEqualityComparer.Default.Equals(attrClass, orchestratorAttr))
                CheckOrchestrator(context, method, orchestrationContextType);
            else if (activityAttr is not null && SymbolEqualityComparer.Default.Equals(attrClass, activityAttr))
                CheckActivity(context, method, activityContextType);
            else if (endpointAttr is not null && SymbolEqualityComparer.Default.Equals(attrClass, endpointAttr))
                CheckEndpoint(context, method, attr);
        }
    }

    private static void CheckOrchestrator(
        SymbolAnalysisContext context,
        IMethodSymbol method,
        INamedTypeSymbol? orchestrationContextType)
    {
        if (!IsStaticPublicOrInternal(method))
        {
            Report(context, QylDiagnosticDescriptors.OrchestratorMustBeStatic, method, method.Name);
            return;
        }

        if (orchestrationContextType is null) return;

        if (method.Parameters.Length is 0 ||
            !SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, orchestrationContextType))
        {
            var actual = method.Parameters.Length is 0
                ? "(no parameters)"
                : method.Parameters[0].Type.ToDisplayString();
            Report(context, QylDiagnosticDescriptors.OrchestratorFirstParamMustBeContext, method, method.Name, actual);
        }
    }

    private static void CheckActivity(
        SymbolAnalysisContext context,
        IMethodSymbol method,
        INamedTypeSymbol? activityContextType)
    {
        if (!IsStaticPublicOrInternal(method))
        {
            Report(context, QylDiagnosticDescriptors.ActivityMustBeStatic, method, method.Name);
            return;
        }

        if (method.Parameters.Length <= 2) return;

        var hasContext = activityContextType is not null && method.Parameters.Any(
            p => SymbolEqualityComparer.Default.Equals(p.Type, activityContextType));
        if (method.Parameters.Length > (hasContext ? 2 : 1))
        {
            Report(context, QylDiagnosticDescriptors.ActivityTooManyParameters, method, method.Name, method.Parameters.Length);
        }
    }

    private static void CheckEndpoint(
        SymbolAnalysisContext context,
        IMethodSymbol method,
        AttributeData attr)
    {
        if (attr.ConstructorArguments.Length is 0 ||
            attr.ConstructorArguments[0].Value is not string pattern ||
            string.IsNullOrWhiteSpace(pattern))
        {
            Report(context, QylDiagnosticDescriptors.AgentEndpointPatternEmpty, method, method.Name);
            return;
        }

        var trimmed = pattern.Trim();
        var spaceIdx = trimmed.IndexOf(' ');
        if (spaceIdx <= 0) return;

        var verb = trimmed.Substring(0, spaceIdx).ToUpperInvariant();
        if (!s_supportedVerbs.Contains(verb))
            Report(context, QylDiagnosticDescriptors.AgentEndpointUnsupportedVerb, method, verb);
    }

    private static bool IsStaticPublicOrInternal(IMethodSymbol method) =>
        method is { IsStatic: true, DeclaredAccessibility: Accessibility.Public or Accessibility.Internal };

    private static void Report(
        SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        IMethodSymbol method,
        params object[] args)
    {
        var location = method.Locations.Length > 0 ? method.Locations[0] : Location.None;
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
    }
}
