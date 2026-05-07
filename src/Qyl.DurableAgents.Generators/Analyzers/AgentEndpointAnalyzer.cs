using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Qyl.DurableAgents.Generators.Models;

namespace Qyl.DurableAgents.Generators.Analyzers;

internal static class AgentEndpointAnalyzer
{
    internal const string MetadataName = GeneratorPipelineHelpers.QylAgentEndpointAttributeMetadataName;

    public static bool CouldBeEndpointMethod(SyntaxNode node, CancellationToken _) =>
        node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

    public static AgentEndpointEntry? Extract(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetSymbol is not IMethodSymbol method)
            return null;

        var attr = context.Attributes[0];
        if (attr.ConstructorArguments is not [{ Value: string pattern }, ..] || string.IsNullOrWhiteSpace(pattern))
            return null;

        var (httpMethod, route) = ParsePattern(pattern);
        if (route is null)
            return null;

        string? orchestratorName = null;
        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "Orchestrator" && named.Value.Value is string n && !string.IsNullOrWhiteSpace(n))
                orchestratorName = n;
        }

        orchestratorName ??= method.Name;

        string? inputTypeFqn = null;
        if (method.Parameters.Length >= 1)
            inputTypeFqn = method.Parameters[0].Type.GetFullyQualifiedName();

        return new AgentEndpointEntry(
            httpMethod,
            route,
            orchestratorName,
            inputTypeFqn);
    }

    private static (string HttpMethod, string? Route) ParsePattern(string pattern)
    {
        var trimmed = pattern.Trim();
        var spaceIdx = trimmed.IndexOf(' ');
        if (spaceIdx <= 0)
            return ("POST", trimmed.StartsWithOrdinal("/") ? trimmed : "/" + trimmed);

        var verb = trimmed.Substring(0, spaceIdx).ToUpperInvariant();
        var route = trimmed.Substring(spaceIdx + 1).Trim();
        if (!route.StartsWithOrdinal("/"))
            route = "/" + route;
        return (verb, route);
    }
}
