// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Observability/WorkflowTelemetryContext.cs
// TRIMMED: only WorkflowTelemetryContext.Disabled is used by TestRunContext.
// The full class has 15+ methods for Activity creation + telemetry options — none needed in test harness.

namespace ANcpLua.Agents.Testing.Workflows.Internals;

/// <summary>Minimal stub — TestRunContext returns WorkflowTelemetryContext.Disabled to opt out of telemetry.</summary>
internal sealed class WorkflowTelemetryContext
{
    private WorkflowTelemetryContext()
    {
    }

    public static WorkflowTelemetryContext Disabled { get; } = new();
}