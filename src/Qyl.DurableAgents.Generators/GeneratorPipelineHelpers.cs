using Microsoft.CodeAnalysis;

namespace Qyl.DurableAgents.Generators;

internal static class GeneratorPipelineHelpers
{
    public const string QylOrchestratorAttributeMetadataName = "Qyl.DurableAgents.QylOrchestratorAttribute";
    public const string QylActivityAttributeMetadataName = "Qyl.DurableAgents.QylActivityAttribute";
    public const string QylAgentEndpointAttributeMetadataName = "Qyl.DurableAgents.QylAgentEndpointAttribute";

    public const string TaskOrchestrationContextMetadataName = "Microsoft.DurableTask.TaskOrchestrationContext";
    public const string TaskActivityContextMetadataName = "Microsoft.DurableTask.TaskActivityContext";

    /// <summary>
    /// Gate: only emit the host class when the abstractions assembly is referenced
    /// — guarantees the marker attributes resolve.
    /// </summary>
    public static bool IsRuntimeReferenced(Compilation compilation, CancellationToken _) =>
        compilation.GetTypeByMetadataName(QylOrchestratorAttributeMetadataName) is not null;
}
