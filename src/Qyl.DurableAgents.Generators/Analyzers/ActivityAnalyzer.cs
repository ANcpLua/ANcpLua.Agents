using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Qyl.DurableAgents.Generators.Models;

namespace Qyl.DurableAgents.Generators.Analyzers;

internal static class ActivityAnalyzer
{
    internal const string MetadataName = GeneratorPipelineHelpers.QylActivityAttributeMetadataName;

    public static bool CouldBeActivityMethod(SyntaxNode node, CancellationToken _) =>
        node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

    public static ActivityEntry? Extract(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetSymbol is not IMethodSymbol
            {
                IsStatic: true,
                DeclaredAccessibility: Accessibility.Public or Accessibility.Internal
            } method)
            return null;

        var contextType = context.SemanticModel.Compilation
            .GetTypeByMetadataName(GeneratorPipelineHelpers.TaskActivityContextMetadataName);

        var (shape, inputType) = ClassifyParameters(method, contextType);

        var (returnFqn, returnsTask, returnsVoid) = UnwrapReturnType(method);

        var attr = context.Attributes[0];
        string? configuredName = null;
        if (attr.ConstructorArguments is [{ Value: string s }, ..])
            configuredName = s;
        var taskName = string.IsNullOrWhiteSpace(configuredName) ? method.Name : configuredName!;

        return new ActivityEntry(
            taskName,
            method.ContainingType.GetFullyQualifiedName(),
            method.Name,
            inputType,
            returnFqn,
            returnsTask,
            returnsVoid,
            shape);
    }

    private static (ActivityParameterShape Shape, string? InputTypeFqn) ClassifyParameters(
        IMethodSymbol method,
        INamedTypeSymbol? contextType)
    {
        var parameters = method.Parameters;
        if (parameters.Length is 0)
            return (ActivityParameterShape.None, null);

        bool IsContext(IParameterSymbol p) =>
            contextType is not null && SymbolEqualityComparer.Default.Equals(p.Type, contextType);

        if (parameters.Length is 1)
        {
            var only = parameters[0];
            return IsContext(only)
                ? (ActivityParameterShape.ContextOnly, null)
                : (ActivityParameterShape.InputOnly, only.Type.GetFullyQualifiedName());
        }

        var first = parameters[0];
        var second = parameters[1];

        if (IsContext(first))
            return (ActivityParameterShape.ContextThenInput, second.Type.GetFullyQualifiedName());

        if (IsContext(second))
            return (ActivityParameterShape.InputThenContext, first.Type.GetFullyQualifiedName());

        return (ActivityParameterShape.InputOnly, first.Type.GetFullyQualifiedName());
    }

    private static (string ReturnFqn, bool ReturnsTask, bool ReturnsVoid) UnwrapReturnType(IMethodSymbol method)
    {
        if (method.ReturnsVoid)
            return ("global::System.ValueTuple", false, true);

        if (method.ReturnType is INamedTypeSymbol named && named.IsGenericType)
        {
            var def = named.ConstructedFrom.ToDisplayString();
            if (def is "System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>")
            {
                return (named.TypeArguments[0].GetFullyQualifiedName(), true, false);
            }
        }

        var fqn = method.ReturnType.ToDisplayString();
        if (fqn is "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask")
            return ("global::System.ValueTuple", true, true);

        return (method.ReturnType.GetFullyQualifiedName(), false, false);
    }
}
