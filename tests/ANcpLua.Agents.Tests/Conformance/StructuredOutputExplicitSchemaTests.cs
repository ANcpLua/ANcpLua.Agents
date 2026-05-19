using System.Text.Json;
using System.Text.Json.Serialization;
using ANcpLua.Agents.Facades;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Conformance;

/// <summary>
/// Behavioral tests for <see cref="QylSchemaExtensions.RunQylWithSchemaAsync{T}"/>. The wrapper's
/// value-add over MAF's stock <c>RunAsync&lt;T&gt;</c> is automatic <see cref="JsonStringEnumConverter"/>
/// injection so enum-bearing DTOs deserialize cleanly without each consumer wiring the converter
/// by hand. These tests pin the auto-injection contract end-to-end via a <see cref="FakeChatClient"/>
/// that returns canned JSON.
/// </summary>
public sealed class StructuredOutputExplicitSchemaTests
{
    private sealed class TrafficLightReading
    {
        public TrafficLightColor Colour { get; set; }
    }

    private enum TrafficLightColor
    {
        Red,
        Yellow,
        Green
    }

    private static ChatClientAgent BuildAgent(FakeChatClient client) =>
        new(client, new ChatClientAgentOptions { Name = "schema-test" });

    [Fact]
    public async Task RunQylWithSchemaAsync_DefaultOptions_DeserializesEnumFromJsonString()
    {
        using var fake = new FakeChatClient();
        fake.WithResponse("""{"Colour":"Red"}""");
        var agent = BuildAgent(fake);

        var response = await agent.RunQylWithSchemaAsync<TrafficLightReading>("any prompt");

        response.Should().NotBeNull();
        response.Result.Should().NotBeNull();
        response.Result.Colour.Should().Be(TrafficLightColor.Red);
    }

    [Fact]
    public async Task RunQylWithSchemaAsync_CallerOptionsWithoutConverter_InjectsConverterInPlace()
    {
        using var fake = new FakeChatClient();
        fake.WithResponse("""{"Colour":"Green"}""");
        var agent = BuildAgent(fake);
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        await agent.RunQylWithSchemaAsync<TrafficLightReading>("any prompt", jsonOptions: opts);

        opts.Converters.OfType<JsonStringEnumConverter>().Should().ContainSingle();
    }

    [Fact]
    public async Task RunQylWithSchemaAsync_CallerOptionsWithExistingConverter_DoesNotDuplicate()
    {
        using var fake = new FakeChatClient();
        fake.WithResponse("""{"Colour":"Yellow"}""");
        var agent = BuildAgent(fake);
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opts.Converters.Add(new JsonStringEnumConverter());

        await agent.RunQylWithSchemaAsync<TrafficLightReading>("any prompt", jsonOptions: opts);

        opts.Converters.OfType<JsonStringEnumConverter>().Should().ContainSingle();
    }

    [Fact]
    public async Task RunQylWithSchemaAsync_AutoEnumConverterDisabled_DoesNotInject()
    {
        using var fake = new FakeChatClient();
        fake.WithResponse("""{"Colour":0}""");
        var agent = BuildAgent(fake);
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        await agent.RunQylWithSchemaAsync<TrafficLightReading>(
            "any prompt",
            autoEnumConverter: false,
            jsonOptions: opts);

        opts.Converters.OfType<JsonStringEnumConverter>().Should().BeEmpty();
    }

    [Fact]
    public async Task RunQylWithSchemaAsync_NullAgent_Throws()
    {
        ChatClientAgent agent = null!;

        var act = () => agent.RunQylWithSchemaAsync<TrafficLightReading>("any prompt");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunQylWithSchemaAsync_InvalidInput_ThrowsArgumentException(string input)
    {
        using var fake = new FakeChatClient();
        fake.WithResponse("""{"Colour":"Red"}""");
        var agent = BuildAgent(fake);

        var act = () => agent.RunQylWithSchemaAsync<TrafficLightReading>(input);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunQylWithSchemaAsync_InvokesUnderlyingAgentRunAsync()
    {
        using var fake = new FakeChatClient();
        fake.WithResponse("""{"Colour":"Red"}""");
        var agent = BuildAgent(fake);

        await agent.RunQylWithSchemaAsync<TrafficLightReading>("any prompt");

        fake.CallCount.Should().Be(1);
        fake.LastOptions.Should().NotBeNull();
    }
}
