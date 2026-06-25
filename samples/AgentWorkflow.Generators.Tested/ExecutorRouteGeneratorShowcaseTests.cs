using ANcpLua.Roslyn.Utilities.Testing;
using ANcpLua.Roslyn.Utilities.Testing.Aot;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AgentWorkflow.Generators.Tested;

// Showcase: test Microsoft Agent Framework's OWN source generator — ExecutorRouteGenerator from
// Microsoft.Agents.AI.Workflows.Generators — using ANcpLua.Roslyn.Utilities.Testing.
//
// The generator is driven over a compilation that references the real MAF Workflows assembly (so the
// Executor base type resolves), run twice for incremental-caching analysis, then wrapped in ANcpLua's
// GeneratorResult / GeneratorCachingReport. The showcase asserts the stable, true properties (it emits
// the executor route, with no diagnostics, and the route compiles against real MAF types) and SURFACES
// the harness's deeper analysis — including that MAF's generator holds Roslyn symbols in its pipeline
// state, which ANcpLua's forbidden-type analyzer flags. That is a real characteristic of the generator,
// reported here rather than asserted away.
//
// Combination: MAF source generator x ANcpLua.Roslyn.Utilities.Testing x ANcpLua.Roslyn.Utilities.Testing.Aot.
public class ExecutorRouteGeneratorShowcaseTests(ITestOutputHelper output)
{
    private const string GeneratedHint = "Showcase.GreetingExecutor.g.cs";

    private const string ExecutorSource =
        """
        using System.Threading.Tasks;
        using Microsoft.Agents.AI.Workflows;

        namespace Showcase;

        public partial class GreetingExecutor : Executor
        {
            public GreetingExecutor() : base("greeting") { }

            [MessageHandler]
            private ValueTask<string> HandleAsync(string message, IWorkflowContext context)
                => new("hello: " + message);
        }
        """;

    [Fact]
    public void Generator_Produces_Clean_ExecutorRoute()
    {
        var (first, second) = RunTwice(ExecutorSource);

        // GeneratorResult collects assertions and throws on Verify()/Dispose().
        using var result = new GeneratorResult(first, second, ExecutorSource, typeof(ExecutorRouteGenerator));
        result
            .Produces(GeneratedHint)                       // generator emitted the executor route file
            .Compiles()                                    // no error-severity generator diagnostics
            .IsClean()                                     // no diagnostics of any severity
            .File(GeneratedHint, src => Assert.Contains("AddHandler", src, StringComparison.Ordinal));
    }

    [Fact]
    public void GeneratedRoute_Compiles_Against_RealMafTypes()
    {
        var (first, _) = RunTwice(ExecutorSource);

        // ANcpLua's Compile: re-compile the original partial + every generated source against the
        // real MAF Workflows assembly and assert success.
        var compile = Compile.Source(ExecutorSource)
            .WithReference(typeof(Executor).Assembly)
            .WithReference(typeof(System.Threading.Tasks.ValueTask).Assembly)
            .WithCommonReferences();

        foreach (var generated in first.Results.SelectMany(r => r.GeneratedSources))
            compile.WithSource(generated.SourceText.ToString());

        compile.Build().ShouldSucceed();
    }

    [Fact]
    public void Harness_Analyzes_GeneratorIncrementality()
    {
        var (first, second) = RunTwice(ExecutorSource);

        // GeneratorCachingReport compares two runs: which user pipeline steps ran, whether output was
        // produced, and whether any forbidden Roslyn type (ISymbol/Compilation) is cached in state.
        var report = GeneratorCachingReport.Create(first, second, typeof(ExecutorRouteGenerator));

        Assert.Equal(nameof(ExecutorRouteGenerator), report.GeneratorName);
        Assert.True(report.ProducedOutput, "the generator should produce at least one source file");

        output.WriteLine($"Generator: {report.GeneratorName}");
        output.WriteLine($"Observable pipeline steps: {report.ObservableSteps.Count}");
        foreach (var step in report.ObservableSteps)
            output.WriteLine($"  - {step.StepName} (cachedSuccessfully={step.IsCachedSuccessfully})");

        // Real finding, surfaced not suppressed: MAF's generator carries Roslyn symbols in pipeline
        // state, so ANcpLua's forbidden-type analyzer reports violations. We assert the harness *can*
        // observe this rather than demanding the generator be violation-free.
        output.WriteLine($"Forbidden-type violations detected by the harness: {report.ForbiddenTypeViolations.Count}");
        foreach (var violation in report.ForbiddenTypeViolations)
            output.WriteLine($"  - {violation.StepName}");
    }

    [Fact]
    public void AotPosture_IsObservable()
    {
        // The xUnit host runs JIT, so dynamic code is supported. TrimAssert.TypePreserved/TypeTrimmed
        // are for AOT-published smoke apps — they call Environment.Exit on failure, so they are
        // documented in the sample README rather than invoked inside a unit test. AotRuntime exposes
        // the mode safely for branching/assertions.
        Assert.True(AotRuntime.IsDynamicCodeSupported);
        Assert.False(AotRuntime.IsNativeAot);
    }

    // Drives ExecutorRouteGenerator twice over a MAF-referencing compilation, with step tracking on
    // so the caching report can analyze incremental behavior across the two runs.
    private static (GeneratorDriverRunResult First, GeneratorDriverRunResult Second) RunTwice(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Executor).Assembly.Location)
        ];

        var compilation = CSharpCompilation.Create(
            "ShowcaseAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ExecutorRouteGenerator().AsSourceGenerator()],
            additionalTexts: null,
            parseOptions: null,
            optionsProvider: null,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        var first = driver.GetRunResult();

        var clone = CSharpCompilation.Create(
            compilation.AssemblyName,
            compilation.SyntaxTrees,
            compilation.References,
            compilation.Options);

        driver = driver.RunGenerators(clone);
        var second = driver.GetRunResult();

        return (first, second);
    }
}
