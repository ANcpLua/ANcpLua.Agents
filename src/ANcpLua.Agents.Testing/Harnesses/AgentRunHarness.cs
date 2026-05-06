using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Xunit;
using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Agents.Testing.Harnesses;

/// <summary>
///     Entry point for focused agent run tests. Conformance suites remain the provider contract;
///     this harness covers direct arrange/run/assert cases without fixture inheritance.
/// </summary>
public static class AgentRunHarness
{
    /// <summary>Creates a run harness builder around an existing agent instance.</summary>
    public static AgentRunHarnessBuilder For(AIAgent agent)
    {
        Guard.NotNull(agent);
        return new AgentRunHarnessBuilder(agent);
    }
}

/// <summary>
///     Fluent builder for running a single <see cref="AIAgent" /> turn with optional
///     session, run options, and input messages.
/// </summary>
public sealed class AgentRunHarnessBuilder
{
    private readonly AIAgent _agent;
    private readonly List<ChatMessage> _messages = [];
    private AgentRunOptions? _options;
    private AgentSession? _session;

    internal AgentRunHarnessBuilder(AIAgent agent)
    {
        _agent = agent;
    }

    /// <summary>Uses an existing session instead of creating a fresh one.</summary>
    public AgentRunHarnessBuilder WithSession(AgentSession session)
    {
        Guard.NotNull(session);
        _session = session;
        return this;
    }

    /// <summary>Uses the supplied run options for the run.</summary>
    public AgentRunHarnessBuilder WithOptions(AgentRunOptions? options)
    {
        _options = options;
        return this;
    }

    /// <summary>Adds a user message to the run input.</summary>
    public AgentRunHarnessBuilder WithUserMessage(string text)
    {
        _messages.Add(new ChatMessage(ChatRole.User, text));
        return this;
    }

    /// <summary>Adds a chat message to the run input.</summary>
    public AgentRunHarnessBuilder WithMessage(ChatMessage message)
    {
        Guard.NotNull(message);
        _messages.Add(message);
        return this;
    }

    /// <summary>Adds multiple chat messages to the run input.</summary>
    public AgentRunHarnessBuilder WithMessages(IEnumerable<ChatMessage> messages)
    {
        Guard.NotNull(messages);
        _messages.AddRange(messages);
        return this;
    }

    /// <summary>Runs the configured input through <c>AIAgent.RunAsync</c>.</summary>
    public async Task<AgentRunHarnessResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var session = _session ?? await _agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);

        var response = _messages.Count == 0
            ? await _agent.RunAsync(session, _options, cancellationToken).ConfigureAwait(false)
            : await _agent.RunAsync(_messages, session, _options, cancellationToken).ConfigureAwait(false);

        return new AgentRunHarnessResult(_agent, session, response, [.. _messages], _session is null);
    }

    /// <summary>Runs the configured input through <c>AIAgent.RunStreamingAsync</c> and materializes all updates.</summary>
    public async Task<AgentStreamingRunHarnessResult> RunStreamingAsync(CancellationToken cancellationToken = default)
    {
        var session = _session ?? await _agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);

        var updates = new List<AgentResponseUpdate>();
        var stream = _messages.Count == 0
            ? _agent.RunStreamingAsync(session, _options, cancellationToken)
            : _agent.RunStreamingAsync(_messages, session, _options, cancellationToken);

        await foreach (var update in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
            updates.Add(update);

        return new AgentStreamingRunHarnessResult(_agent, session, updates, [.. _messages], _session is null);
    }
}

/// <summary>Materialized result of a non-streaming agent run harness.</summary>
public sealed record AgentRunHarnessResult(
    AIAgent Agent,
    AgentSession Session,
    AgentResponse Response,
    IReadOnlyList<ChatMessage> InputMessages,
    bool CreatedSession)
{
    /// <summary>Starts fluent assertions over the run result.</summary>
    public AgentRunHarnessAssertions Should()
    {
        return new AgentRunHarnessAssertions(this);
    }
}

/// <summary>Materialized result of a streaming agent run harness.</summary>
public sealed record AgentStreamingRunHarnessResult(
    AIAgent Agent,
    AgentSession Session,
    IReadOnlyList<AgentResponseUpdate> Updates,
    IReadOnlyList<ChatMessage> InputMessages,
    bool CreatedSession)
{
    /// <summary>Concatenated text across all materialized streaming updates.</summary>
    public string Text => string.Concat(Updates.Select(static update => update.Text));

    /// <summary>Starts fluent assertions over the streaming run result.</summary>
    public AgentStreamingRunHarnessAssertions Should()
    {
        return new AgentStreamingRunHarnessAssertions(this);
    }
}

/// <summary>Small xUnit-backed assertion wrapper for <see cref="AgentRunHarnessResult" />.</summary>
public readonly struct AgentRunHarnessAssertions(AgentRunHarnessResult result)
{
    public AgentRunHarnessAssertions And => this;

    public AgentRunHarnessAssertions HaveTextContaining(string expected)
    {
        Assert.Contains(expected, result.Response.Text, StringComparison.Ordinal);
        return this;
    }

    public AgentRunHarnessAssertions HaveMessageCount(int expected)
    {
        Assert.Equal(expected, result.Response.Messages.Count);
        return this;
    }

    public AgentRunHarnessAssertions HaveAgentId(string? expected)
    {
        Assert.Equal(expected, result.Response.AgentId);
        return this;
    }
}

/// <summary>Small xUnit-backed assertion wrapper for <see cref="AgentStreamingRunHarnessResult" />.</summary>
public readonly struct AgentStreamingRunHarnessAssertions(AgentStreamingRunHarnessResult result)
{
    public AgentStreamingRunHarnessAssertions And => this;

    public AgentStreamingRunHarnessAssertions HaveTextContaining(string expected)
    {
        Assert.Contains(expected, result.Text, StringComparison.Ordinal);
        return this;
    }

    public AgentStreamingRunHarnessAssertions HaveUpdateCount(int expected)
    {
        Assert.Equal(expected, result.Updates.Count);
        return this;
    }

    public AgentStreamingRunHarnessAssertions HaveAnyUpdates()
    {
        Assert.NotEmpty(result.Updates);
        return this;
    }
}
