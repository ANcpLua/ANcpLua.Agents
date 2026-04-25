// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.Generators.UnitTests/SyntaxTreeFluentExtensions.cs

using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Agents.Testing.Workflows;

// Fluent assertions over a generated SyntaxTree. Read the assertions as a spec
// for what the incremental generator is expected to emit:
//
//   generatedTree.Should()
//                .AddHandler<FooMessage, BarResult>("this.HandleFooAsync")
//                .And.RegisterSentMessageType<BarResult>()
//                .And.RegisterYieldedOutputType<FinalResult>()
//                .And.HaveHierarchy("OuterClass", "InnerExecutor");
internal sealed class SyntaxTreeAssertions : ObjectAssertions<SyntaxTree, SyntaxTreeAssertions>
{
    private readonly string _syntaxString;

    public SyntaxTreeAssertions(SyntaxTree instance, AssertionChain assertionChain) : base(instance, assertionChain)
    {
        _syntaxString = instance.ToString();
    }

    public AndConstraint<SyntaxTreeAssertions> AddHandler(string handlerName)
    {
        return Match($".AddHandler({handlerName})", $"expected handler {handlerName} to be registered");
    }

    public AndConstraint<SyntaxTreeAssertions> AddHandler(string handlerName, string inTypeParam)
    {
        return Match($".AddHandler<{inTypeParam}>({handlerName})", $"expected handler {handlerName} to be registered");
    }

    public AndConstraint<SyntaxTreeAssertions> AddHandler(string handlerName, string inTypeParam, string outTypeParam)
    {
        return Match($".AddHandler<{inTypeParam},{outTypeParam}>({handlerName})",
            $"expected handler {handlerName} to be registered");
    }

    public AndConstraint<SyntaxTreeAssertions> AddHandler<TIn>(string handlerName, bool globalQualified = false)
    {
        return AddHandler(handlerName, TypeParam<TIn>(globalQualified));
    }

    public AndConstraint<SyntaxTreeAssertions> AddHandler<TIn, TOut>(string handlerName, bool globalQualified = false)
    {
        return AddHandler(handlerName, TypeParam<TIn>(globalQualified), TypeParam<TOut>(globalQualified));
    }

    public AndConstraint<SyntaxTreeAssertions> HaveNoHandlers()
    {
        return MatchAbsent(".AddHandler(", "expected no handlers to be registered");
    }

    public AndConstraint<SyntaxTreeAssertions> RegisterSentMessageType(string messageTypeParam)
    {
        return Match($".SendsMessage<{messageTypeParam}>()",
            $"expected message type {messageTypeParam} to be registered");
    }

    public AndConstraint<SyntaxTreeAssertions> RegisterSentMessageType<TMessage>(bool globalQualified = true)
    {
        return RegisterSentMessageType(TypeParam<TMessage>(globalQualified));
    }

    public AndConstraint<SyntaxTreeAssertions> NotRegisterSentMessageTypes()
    {
        return MatchAbsent(".SendsMessage<", "expected no message types to be registered");
    }

    public AndConstraint<SyntaxTreeAssertions> RegisterYieldedOutputType(string outputTypeParam)
    {
        return Match($".YieldsOutput<{outputTypeParam}>()", $"expected output type {outputTypeParam} to be registered");
    }

    public AndConstraint<SyntaxTreeAssertions> RegisterYieldedOutputType<TOutput>(bool globalQualified = true)
    {
        return RegisterYieldedOutputType(TypeParam<TOutput>(globalQualified));
    }

    public AndConstraint<SyntaxTreeAssertions> NotRegisterYieldedOutputTypes()
    {
        return MatchAbsent(".YieldsOutput<", "expected no output types to be registered");
    }

    public AndConstraint<SyntaxTreeAssertions> HaveNamespace()
    {
        return Match("namespace ", "expected namespace declaration");
    }

    public AndConstraint<SyntaxTreeAssertions> NotHaveNamespace()
    {
        return MatchAbsent("namespace ", "expected no namespace declaration");
    }

    public AndConstraint<SyntaxTreeAssertions> HaveHierarchy(params string[] expectedNesting)
    {
        if (expectedNesting.Length is 0) return new AndConstraint<SyntaxTreeAssertions>(this);

        var indicies = new int[expectedNesting.Length];
        for (var i = 0; i < expectedNesting.Length; i++)
            indicies[i] = _syntaxString.IndexOfOrdinal($"partial class {expectedNesting[i]}");

        var runningResult = Contain(0, indicies[0], expectedNesting[0]);
        for (var i = 1; i < expectedNesting.Length; i++)
            runningResult = runningResult.And.Contain(i, indicies[i], expectedNesting[i])
                .And.InOrder(indicies[i - 1], indicies[i], expectedNesting[i - 1], expectedNesting[i]);
        return runningResult;
    }

    private AndConstraint<SyntaxTreeAssertions> Match(string expected, string reason)
    {
        CurrentAssertionChain
            .ForCondition(_syntaxString.ContainsOrdinal(expected))
            .BecauseOf(reason)
            .FailWith("Expected {context} to contain {0}{reason}, but it was not found. Actual syntax: {1}", expected,
                _syntaxString);
        return new AndConstraint<SyntaxTreeAssertions>(this);
    }

    private AndConstraint<SyntaxTreeAssertions> MatchAbsent(string needle, string reason)
    {
        CurrentAssertionChain
            .ForCondition(!_syntaxString.ContainsOrdinal(needle))
            .BecauseOf(reason)
            .FailWith("Expected {context} to not contain {0}{reason}. Actual syntax: {1}", needle, _syntaxString);
        return new AndConstraint<SyntaxTreeAssertions>(this);
    }

    private AndConstraint<SyntaxTreeAssertions> Contain(int level, int index, string className)
    {
        CurrentAssertionChain
            .ForCondition(index > 0)
            .BecauseOf($"expected \"partial class {className}\" at nesting level {level}")
            .FailWith("Expected {context} to contain partial class {0} at level {1}{reason}. Actual syntax: {2}",
                className, level, _syntaxString);
        return new AndConstraint<SyntaxTreeAssertions>(this);
    }

    private AndConstraint<SyntaxTreeAssertions> InOrder(int prev, int curr, string prevClass, string currClass)
    {
        CurrentAssertionChain
            .ForCondition(prev < curr)
            .BecauseOf($"expected \"partial class {prevClass}\" before \"partial class {currClass}\"")
            .FailWith("Expected {context} to declare {0} before {1}{reason}. Actual syntax: {2}", prevClass, currClass,
                _syntaxString);
        return new AndConstraint<SyntaxTreeAssertions>(this);
    }

    private static string TypeParam<T>(bool globalQualified)
    {
        var type = typeof(T);
        return globalQualified ? $"global::{type.FullName}" : type.Name;
    }
}

internal static class SyntaxTreeFluentExtensions
{
    public static SyntaxTreeAssertions Should(this SyntaxTree syntaxTree)
    {
        return new SyntaxTreeAssertions(syntaxTree, AssertionChain.GetOrCreate());
    }
}