using System.Text.Json;
using System.Text.Json.Serialization;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Facades;

/// <summary>
///     Explicit-schema runner for <see cref="AIAgent"/> with enum-friendly defaults.
///     MAF's built-in <c>RunAsync&lt;T&gt;</c> handles the structured-output path; this wrapper
///     adds a pre-configured <see cref="JsonSerializerOptions"/> with
///     <see cref="JsonStringEnumConverter"/> attached so enum-heavy domains "just work".
/// </summary>
public static class QylSchemaExtensions
{
    /// <summary>
    ///     Runs <paramref name="agent"/> against <paramref name="input"/> and deserializes the
    ///     result into <typeparamref name="T"/>. When <paramref name="autoEnumConverter"/> is
    ///     <c>true</c> (default), a <see cref="JsonStringEnumConverter"/> is added to
    ///     <paramref name="jsonOptions"/> if not already present.
    /// </summary>
    public static Task<AgentResponse<T>> RunQylWithSchemaAsync<T>(
        this AIAgent agent,
        string input,
        bool autoEnumConverter = true,
        JsonSerializerOptions? jsonOptions = null,
        AgentSession? session = null,
        AgentRunOptions? runOptions = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(agent);
        Guard.NotNullOrWhiteSpace(input);

        var opts = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        if (autoEnumConverter && !opts.Converters.OfType<JsonStringEnumConverter>().Any())
            opts.Converters.Add(new JsonStringEnumConverter());

        return agent.RunAsync<T>(input, session, opts, runOptions, cancellationToken);
    }
}
