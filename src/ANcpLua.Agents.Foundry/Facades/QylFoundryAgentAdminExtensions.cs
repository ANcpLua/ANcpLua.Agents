using System.ClientModel;
using ANcpLua.Roslyn.Utilities;
using Azure.AI.Projects.Agents;

namespace ANcpLua.Agents.Foundry;

/// <summary>
///     <c>Qyl</c>-prefixed one-liner wrappers over <see cref="AgentAdministrationClient" /> — the
///     server-side Foundry agent lifecycle (create / read / list / delete of agents and their
///     versions). Complements the consumption-side facades (<c>AsQylAIAgent</c> /
///     <c>AsQylVersionedAIAgent</c>), which run an <em>already-published</em> agent version.
/// </summary>
/// <remarks>
///     Each method forwards verbatim to the underlying client (validation, retries, and paging are
///     inherited unchanged); the wrappers add <c>Qyl</c> naming, null-guarding, and a single
///     <see cref="CancellationToken" /> tail parameter so the lifecycle reads cohesively next to the
///     rest of <c>ANcpLua.Agents.Foundry</c>. Construct the <see cref="AgentAdministrationClient" />
///     directly (endpoint + credential); 2.1.0-beta.2 does not surface it off <c>AIProjectClient</c>.
/// </remarks>
public static class QylFoundryAgentAdminExtensions
{
    /// <summary>Publishes a new version of an agent.</summary>
    public static Task<ClientResult<ProjectsAgentVersion>> CreateQylAgentVersionAsync(
        this AgentAdministrationClient admin,
        string agentName,
        ProjectsAgentVersionCreationOptions options,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(admin);
        Guard.NotNullOrWhiteSpace(agentName);
        Guard.NotNull(options);
        return admin.CreateAgentVersionAsync(agentName, options, cancellationToken: cancellationToken);
    }

    /// <summary>Reads a single agent by name.</summary>
    public static Task<ClientResult<ProjectsAgentRecord>> GetQylAgentAsync(
        this AgentAdministrationClient admin,
        string agentName,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(admin);
        Guard.NotNullOrWhiteSpace(agentName);
        return admin.GetAgentAsync(agentName, cancellationToken);
    }

    /// <summary>Reads a specific version of an agent.</summary>
    public static Task<ClientResult<ProjectsAgentVersion>> GetQylAgentVersionAsync(
        this AgentAdministrationClient admin,
        string agentName,
        string agentVersion,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(admin);
        Guard.NotNullOrWhiteSpace(agentName);
        Guard.NotNullOrWhiteSpace(agentVersion);
        return admin.GetAgentVersionAsync(agentName, agentVersion, cancellationToken);
    }

    /// <summary>Lists all agents in the project.</summary>
    public static AsyncCollectionResult<ProjectsAgentRecord> GetQylAgentsAsync(
        this AgentAdministrationClient admin,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(admin);
        return admin.GetAgentsAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Lists every published version of an agent.</summary>
    public static AsyncCollectionResult<ProjectsAgentVersion> GetQylAgentVersionsAsync(
        this AgentAdministrationClient admin,
        string agentName,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(admin);
        Guard.NotNullOrWhiteSpace(agentName);
        return admin.GetAgentVersionsAsync(agentName, cancellationToken: cancellationToken);
    }

    /// <summary>Deletes an agent and all of its versions.</summary>
    public static Task<ClientResult> DeleteQylAgentAsync(
        this AgentAdministrationClient admin,
        string agentName,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(admin);
        Guard.NotNullOrWhiteSpace(agentName);
        return admin.DeleteAgentAsync(agentName, cancellationToken);
    }

    /// <summary>Deletes a single version of an agent.</summary>
    public static Task<ClientResult> DeleteQylAgentVersionAsync(
        this AgentAdministrationClient admin,
        string agentName,
        string agentVersion,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(admin);
        Guard.NotNullOrWhiteSpace(agentName);
        Guard.NotNullOrWhiteSpace(agentVersion);
        return admin.DeleteAgentVersionAsync(agentName, agentVersion, cancellationToken);
    }
}
