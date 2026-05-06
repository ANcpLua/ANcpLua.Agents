using System;
using System.Collections.Generic;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Agents.Workflows;

/// <summary>
///     <c>Qyl</c>-prefixed factories for the public Executor base classes:
///     <see cref="FunctionExecutor{TInput, TOutput}" />, <see cref="AggregatingExecutor{TInput, TAggregate}" />,
///     and typed <see cref="AIAgent" /> bridges.
/// </summary>
public static class QylExecutorFactoryExtensions
{
    /// <summary>
    ///     Wraps a synchronous <paramref name="handler" /> as a
    ///     <see cref="FunctionExecutor{TInput, TOutput}" />.
    /// </summary>
    public static FunctionExecutor<TInput, TOutput> QylFunction<TInput, TOutput>(
        string id,
        Func<TInput, TOutput> handler)
    {
        Guard.NotNullOrWhiteSpace(id);
        Guard.NotNull(handler);
        return new FunctionExecutor<TInput, TOutput>(id, (input, _, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return new ValueTask<TOutput>(handler(input));
        });
    }

    /// <summary>
    ///     Wraps an asynchronous <paramref name="handler" /> as a
    ///     <see cref="FunctionExecutor{TInput, TOutput}" />.
    /// </summary>
    public static FunctionExecutor<TInput, TOutput> QylFunctionAsync<TInput, TOutput>(
        string id,
        Func<TInput, IWorkflowContext, CancellationToken, ValueTask<TOutput>> handler)
    {
        Guard.NotNullOrWhiteSpace(id);
        Guard.NotNull(handler);
        return new FunctionExecutor<TInput, TOutput>(id, handler);
    }

    /// <summary>
    ///     Aggregates incoming <typeparamref name="TInput" /> values into a
    ///     growing <see cref="List{T}" />.
    /// </summary>
    public static AggregatingExecutor<TInput, List<TInput>> QylCollect<TInput>(string id)
    {
        Guard.NotNullOrWhiteSpace(id);
        return new AggregatingExecutor<TInput, List<TInput>>(id, static (acc, value) =>
        {
            List<TInput> list = acc ?? [];
            list.Add(value);
            return list;
        });
    }

    /// <summary>
    ///     Aggregates incoming <see cref="int" /> values by summation.
    /// </summary>
    public static AggregatingExecutor<int, int> QylSum(string id)
    {
        Guard.NotNullOrWhiteSpace(id);
        return new AggregatingExecutor<int, int>(id, static (acc, value) => acc + value);
    }

    /// <summary>
    ///     Wraps an <see cref="AIAgent" /> as a typed
    ///     <see cref="FunctionExecutor{TInput, TOutput}" /> that maps
    ///     <typeparamref name="TInput" /> to a prompt, runs the agent
    ///     with structured output of <typeparamref name="TOutput" />, and
    ///     returns the deserialized <c>AgentResponse&lt;TOutput&gt;.Result</c>.
    /// </summary>
    public static FunctionExecutor<TInput, TOutput> QylAgentExecutor<TInput, TOutput>(
        string id,
        AIAgent agent,
        Func<TInput, string> prompt)
        where TInput : notnull
        where TOutput : notnull
    {
        Guard.NotNullOrWhiteSpace(id);
        Guard.NotNull(agent);
        Guard.NotNull(prompt);
        return new FunctionExecutor<TInput, TOutput>(id, async (input, _, ct) =>
        {
            AgentResponse<TOutput> response = await agent
                .RunAsync<TOutput>(prompt(input), cancellationToken: ct)
                .ConfigureAwait(false);
            return response.Result;
        });
    }

    /// <summary>
    ///     Wraps an <see cref="AIAgent" /> as a typed
    ///     <c>FunctionExecutor&lt;TInput, string&gt;</c> that maps
    ///     <typeparamref name="TInput" /> to a prompt and returns the
    ///     agent's text response.
    /// </summary>
    public static FunctionExecutor<TInput, string> QylAgentExecutor<TInput>(
        string id,
        AIAgent agent,
        Func<TInput, string> prompt)
        where TInput : notnull
    {
        Guard.NotNullOrWhiteSpace(id);
        Guard.NotNull(agent);
        Guard.NotNull(prompt);
        return new FunctionExecutor<TInput, string>(id, async (input, _, ct) =>
        {
            AgentResponse response = await agent
                .RunAsync(prompt(input), cancellationToken: ct)
                .ConfigureAwait(false);
            return response.Text;
        });
    }
}
