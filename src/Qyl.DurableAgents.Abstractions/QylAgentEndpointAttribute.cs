using System;

namespace Qyl.DurableAgents;

/// <summary>
/// Marks a static method as a Minimal-API endpoint that schedules a named
/// orchestration. The constructor pattern is <c>"VERB /route"</c> — for example
/// <c>"POST /reports"</c>. The orchestrator name is supplied via
/// <see cref="Orchestrator"/>; the orchestration input is read from the request
/// body as JSON and the response returns the orchestration instance id.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class QylAgentEndpointAttribute : Attribute
{
    public QylAgentEndpointAttribute(string pattern)
    {
        Pattern = pattern;
    }

    /// <summary>
    /// Verb-and-route pattern such as <c>"POST /reports"</c>.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Name of the orchestration to schedule when the endpoint is invoked.
    /// </summary>
    public string? Orchestrator { get; set; }
}
