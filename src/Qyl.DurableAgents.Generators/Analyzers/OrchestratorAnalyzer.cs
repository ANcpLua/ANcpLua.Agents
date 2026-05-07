using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Qyl.DurableAgents.Generators.Models;

namespace Qyl.DurableAgents.Generators.Analyzers;

internal static class OrchestratorAnalyzer
{
    internal const string MetadataName = GeneratorPipelineHelpers.QylOrchestratorAttributeMetadataName;

    public static bool CouldBeOrchestratorMethod(SyntaxNode node, CancellationToken _) =>
        node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

    public static OrchestratorEntry? Extract(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetSymbol is not IMethodSymbol
            {
                IsStatic: true,
                DeclaredAccessibility: Accessibility.Public or Accessibility.Internal,
                Parameters.Length: >= 1
            } method)
            return null;

        var contextType = context.SemanticModel.Compilation
            .GetTypeByMetadataName(GeneratorPipelineHelpers.TaskOrchestrationContextMetadataName);
        if (contextType is null)
            return null;

        var firstParam = method.Parameters[0];
        if (!SymbolEqualityComparer.Default.Equals(firstParam.Type, contextType))
            return null;

        string? inputTypeFqn = null;
        if (method.Parameters.Length >= 2)
            inputTypeFqn = method.Parameters[1].Type.GetFullyQualifiedName();

        var (returnFqn, returnsTask) = UnwrapReturnType(method);

        var attr = context.Attributes[0];
        string? configuredName = null;
        if (attr.ConstructorArguments is [{ Value: string s }, ..])
            configuredName = s;
        var taskName = string.IsNullOrWhiteSpace(configuredName) ? method.Name : configuredName!;

        return new OrchestratorEntry(
            taskName,
            method.ContainingType.GetFullyQualifiedName(),
            method.Name,
            inputTypeFqn,
            returnFqn,
            returnsTask);
    }

    private static (string ReturnFqn, bool ReturnsTask) UnwrapReturnType(IMethodSymbol method)
    {
        if (method.ReturnsVoid)
            return ("global::System.ValueTuple", false);

        if (method.ReturnType is INamedTypeSymbol named && named.IsGenericType)
        {
            var def = named.ConstructedFrom.ToDisplayString();
            if (def is "System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>")
            {
                return (named.TypeArguments[0].GetFullyQualifiedName(), true);
            }
        }

        var fqn = method.ReturnType.ToDisplayString();
        if (fqn is "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask")
            return ("global::System.ValueTuple", true);

        return (method.ReturnType.GetFullyQualifiedName(), false);
    }
}
