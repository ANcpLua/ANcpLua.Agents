using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Qyl.DurableAgents.Generators.Analyzers;
using Qyl.DurableAgents.Generators.Emitters;

namespace Qyl.DurableAgents.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class QylDurableAgentsGenerator : IIncrementalGenerator
{
    private const string GeneratedFileName = "QylDurableHost.g.cs";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var runtimeAvailable = context.CompilationProvider
            .Select(GeneratorPipelineHelpers.IsRuntimeReferenced);

        var orchestrators = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                OrchestratorAnalyzer.MetadataName,
                OrchestratorAnalyzer.CouldBeOrchestratorMethod,
                OrchestratorAnalyzer.Extract)
            .WhereNotNull()
            .Collect();

        var activities = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ActivityAnalyzer.MetadataName,
                ActivityAnalyzer.CouldBeActivityMethod,
                ActivityAnalyzer.Extract)
            .WhereNotNull()
            .Collect();

        var endpoints = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AgentEndpointAnalyzer.MetadataName,
                AgentEndpointAnalyzer.CouldBeEndpointMethod,
                AgentEndpointAnalyzer.Extract)
            .WhereNotNull()
            .Collect();

        var combined = orchestrators
            .Combine(activities)
            .Combine(endpoints)
            .Combine(runtimeAvailable);

        context.RegisterSourceOutput(combined, static (spc, input) =>
        {
            var (((orch, act), ep), runtime) = input;
            if (!runtime) return;

            var source = DurableHostEmitter.Emit(
                orch.AsEquatableArray(),
                act.AsEquatableArray(),
                ep.AsEquatableArray());

            if (string.IsNullOrEmpty(source)) return;

            spc.AddSource(GeneratedFileName, SourceText.From(source, Encoding.UTF8));
        });
    }
}
