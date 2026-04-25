// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/TestRequestAgent.cs

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using AwesomeAssertions;

namespace ANcpLua.Agents.Testing.Workflows;

// Generates N mixed paired/unpaired function-call or tool-approval requests,
// tracks serviced/unserviced state across runs. The canonical shape for
// testing request/response interrupts and checkpoint-resume flows.
public enum TestAgentRequestType
{
    FunctionCall,
    UserInputRequest
}

internal sealed record TestRequestAgentSessionState(
    JsonElement SessionState,
    Dictionary<string, PortableValue> UnservicedRequests,
    HashSet<string> ServicedRequests,
    HashSet<string> PairedRequests);

internal sealed class TestRequestAgent(
    TestAgentRequestType requestType,
    int unpairedRequestCount,
    int pairedRequestCount,
    string? id,
    string? name) : AIAgent
{
    public AgentSession? LastSession { get; set; }

    protected override string? IdCore => id;

    public override string? Name => name;

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<AgentSession>(requestType switch
        {
            TestAgentRequestType.FunctionCall =>
                new TestRequestAgentSession<FunctionCallContent, FunctionResultContent>(),
            TestAgentRequestType.UserInputRequest =>
                new TestRequestAgentSession<ToolApprovalRequestContent, ToolApprovalResponseContent>(),
            _ => throw new NotSupportedException()
        });
    }

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        return this.CreateSessionCoreAsync(cancellationToken);
    }

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        return default;
    }

    protected override Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null,
        AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this.RunStreamingAsync(messages, session, options, cancellationToken)
            .ToAgentResponseAsync(cancellationToken);
    }

    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(IEnumerable<ChatMessage> messages,
        AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        return requestType switch
        {
            TestAgentRequestType.FunctionCall => this.RunStreamingAsync(new FunctionCallStrategy(), messages, session,
                cancellationToken),
            TestAgentRequestType.UserInputRequest => this.RunStreamingAsync(new FunctionApprovalStrategy(), messages,
                session, cancellationToken),
            _ => throw new NotSupportedException($"Unknown AgentRequestType {requestType}")
        };
    }

    // Reservoir sampling: uniformly pick c indices from [0..n) without listing them all.
    // Uses RandomNumberGenerator.GetInt32 (CA5394-clean) — security is not the concern here,
    // but the analyzer treats any System.Random use as insecure under this rule set.
    private static int[] SampleIndicies(int n, int c)
    {
        var result = Enumerable.Range(0, c).ToArray();
        for (var i = c; i < n; i++)
        {
            var radix = RandomNumberGenerator.GetInt32(i);
            if (radix < c) result[radix] = i;
        }

        return result;
    }

    private async IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync<TRequest, TResponse>(
        IRequestResponseStrategy<TRequest, TResponse> strategy,
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TRequest : AIContent
        where TResponse : AIContent
    {
        LastSession = session ??= await CreateSessionAsync(cancellationToken);
        var traSession = ConvertSession<TRequest, TResponse>(session);

        if (traSession.HasSentRequests)
        {
            foreach (var response in messages.SelectMany(m => m.Contents).OfType<TResponse>())
                strategy.ProcessResponse(response, traSession);

            yield return traSession.UnservicedRequests.Count is 0
                ? new AgentResponseUpdate(ChatRole.Assistant, "Done")
                : new AgentResponseUpdate(ChatRole.Assistant, $"Remaining: {traSession.UnservicedRequests.Count}");

            yield break;
        }

        var totalRequestCount = unpairedRequestCount + pairedRequestCount;
        yield return new AgentResponseUpdate(ChatRole.Assistant,
            $"Creating {totalRequestCount} requests, {pairedRequestCount} paired.");

        HashSet<int> servicedIndicies = [.. SampleIndicies(totalRequestCount, pairedRequestCount)];
        var requests = strategy.CreateRequests(totalRequestCount).ToArray();
        List<AIContent> pairedResponses = new(pairedRequestCount);

        for (var i = 0; i < requests.Length; i++)
        {
            var (id, request) = requests[i];
            if (servicedIndicies.Contains(i))
            {
                traSession.PairedRequests.Add(id);
                pairedResponses.Add(strategy.CreatePairedResponse(request));
            }
            else
            {
                traSession.UnservicedRequests.Add(id, request);
            }

            yield return new AgentResponseUpdate(ChatRole.Assistant, [request]);
        }

        yield return new AgentResponseUpdate(ChatRole.Assistant, pairedResponses);
        traSession.HasSentRequests = true;
    }

    private static TestRequestAgentSession<TRequest, TResponse> ConvertSession<TRequest, TResponse>(
        AgentSession session)
        where TRequest : AIContent
        where TResponse : AIContent
    {
        if (session is not TestRequestAgentSession<TRequest, TResponse> traSession)
            throw new ArgumentException(
                $"Bad AgentSession type: Expected {typeof(TestRequestAgentSession<TRequest, TResponse>)}, got {session.GetType()}.",
                nameof(session));
        return traSession;
    }

    internal IEnumerable<ExternalResponse> ValidateUnpairedRequests(List<ExternalRequest> requests)
    {
        List<object> responses = requestType switch
        {
            TestAgentRequestType.FunctionCall =>
            [
                .. ValidateUnpairedRequests(requests.Select(Extract<FunctionCallContent>), new FunctionCallStrategy())
            ],
            TestAgentRequestType.UserInputRequest =>
            [
                .. ValidateUnpairedRequests(requests.Select(Extract<ToolApprovalRequestContent>),
                    new FunctionApprovalStrategy())
            ],
            _ => throw new NotSupportedException($"Unknown AgentRequestType {requestType}")
        };

        return requests.Zip(responses, (req, resp) => req.CreateResponse(resp));

        static TRequest Extract<TRequest>(ExternalRequest request)
        {
            request.TryGetDataAs(out TRequest? content).Should().BeTrue();
            return content!;
        }
    }

    private IEnumerable<TResponse> ValidateUnpairedRequests<TRequest, TResponse>(IEnumerable<TRequest> requests,
        IRequestResponseStrategy<TRequest, TResponse> strategy)
        where TRequest : AIContent
        where TResponse : AIContent
    {
        LastSession.Should().NotBeNull();
        var traSession = ConvertSession<TRequest, TResponse>(LastSession);

        requests.Should().HaveCount(traSession.UnservicedRequests.Count);
        foreach (var request in requests)
        {
            var requestId = RetrieveId(request);
            traSession.UnservicedRequests.Should().ContainKey(requestId);
            yield return strategy.CreatePairedResponse(request);
        }
    }

    private static string RetrieveId<TRequest>(TRequest request) where TRequest : AIContent
    {
        return request switch
        {
            FunctionCallContent fc => fc.CallId,
            ToolApprovalRequestContent ar => ar.RequestId,
            _ => throw new NotSupportedException($"Unknown request type {typeof(TRequest)}")
        };
    }

    private interface IRequestResponseStrategy<TRequest, TResponse>
        where TRequest : AIContent
        where TResponse : AIContent
    {
        IEnumerable<(string, TRequest)> CreateRequests(int count);
        TResponse CreatePairedResponse(TRequest request);
        void ProcessResponse(TResponse response, TestRequestAgentSession<TRequest, TResponse> session);
    }

    private sealed class FunctionCallStrategy : IRequestResponseStrategy<FunctionCallContent, FunctionResultContent>
    {
        public FunctionResultContent CreatePairedResponse(FunctionCallContent request)
        {
            return new FunctionResultContent(request.CallId, request);
        }

        public IEnumerable<(string, FunctionCallContent)> CreateRequests(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var callId = Guid.NewGuid().ToString("N");
                yield return (callId, new FunctionCallContent(callId, "TestFunction"));
            }
        }

        public void ProcessResponse(FunctionResultContent response,
            TestRequestAgentSession<FunctionCallContent, FunctionResultContent> session)
        {
            if (session.UnservicedRequests.TryGetValue(response.CallId, out var request))
            {
                response.Result.As<FunctionCallContent>().Should().Be(request);
                session.ServicedRequests.Add(response.CallId);
                session.UnservicedRequests.Remove(response.CallId);
                return;
            }

            if (session.ServicedRequests.Contains(response.CallId))
                throw new InvalidOperationException($"Seeing duplicate response with id {response.CallId}");

            if (session.PairedRequests.Contains(response.CallId))
                throw new InvalidOperationException(
                    $"Seeing explicit response to initially paired request with id {response.CallId}");

            throw new InvalidOperationException($"Seeing response to nonexistent request with id {response.CallId}");
        }
    }

    private sealed class
        FunctionApprovalStrategy : IRequestResponseStrategy<ToolApprovalRequestContent, ToolApprovalResponseContent>
    {
        public ToolApprovalResponseContent CreatePairedResponse(ToolApprovalRequestContent request)
        {
            return new ToolApprovalResponseContent(request.RequestId, true, request.ToolCall);
        }

        public IEnumerable<(string, ToolApprovalRequestContent)> CreateRequests(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var id = Guid.NewGuid().ToString("N");
                yield return (id, new ToolApprovalRequestContent(id, new FunctionCallContent(id, "TestFunction")));
            }
        }

        public void ProcessResponse(ToolApprovalResponseContent response,
            TestRequestAgentSession<ToolApprovalRequestContent, ToolApprovalResponseContent> session)
        {
            if (session.UnservicedRequests.TryGetValue(response.RequestId, out var request))
            {
                response.Approved.Should().BeTrue();
                ((FunctionCallContent)response.ToolCall).Should().Be((FunctionCallContent)request.ToolCall);
                session.ServicedRequests.Add(response.RequestId);
                session.UnservicedRequests.Remove(response.RequestId);
                return;
            }

            if (session.ServicedRequests.Contains(response.RequestId))
                throw new InvalidOperationException($"Seeing duplicate response with id {response.RequestId}");

            if (session.PairedRequests.Contains(response.RequestId))
                throw new InvalidOperationException(
                    $"Seeing explicit response to initially paired request with id {response.RequestId}");

            throw new InvalidOperationException($"Seeing response to nonexistent request with id {response.RequestId}");
        }
    }

    private sealed class TestRequestAgentSession<TRequest, TResponse> : AgentSession
        where TRequest : AIContent
        where TResponse : AIContent
    {
        public TestRequestAgentSession()
        {
        }

        public TestRequestAgentSession(JsonElement element, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            var state = element.Deserialize<TestRequestAgentSessionState>(jsonSerializerOptions)
                        ?? throw new ArgumentException("Unable to deserialize session state.");

            StateBag = AgentSessionStateBag.Deserialize(state.SessionState);
            UnservicedRequests = state.UnservicedRequests.ToDictionary(static kv => kv.Key, static kv => kv.Value.As<TRequest>()!);
            ServicedRequests = state.ServicedRequests;
            PairedRequests = state.PairedRequests;
        }

        public bool HasSentRequests { get; set; }

        public Dictionary<string, TRequest> UnservicedRequests { get; } = [];

        public HashSet<string> ServicedRequests { get; } = [];

        public HashSet<string> PairedRequests { get; } = [];

        internal JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            var portable = UnservicedRequests.ToDictionary(static kv => kv.Key, static kv => new PortableValue(kv.Value));
            var state = new TestRequestAgentSessionState(StateBag.Serialize(), portable, ServicedRequests,
                PairedRequests);
            return JsonSerializer.SerializeToElement(state, jsonSerializerOptions);
        }
    }
}