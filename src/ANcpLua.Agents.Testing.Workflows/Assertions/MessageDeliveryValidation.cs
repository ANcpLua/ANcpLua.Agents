// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/MessageDeliveryValidation.cs

using AwesomeAssertions;

namespace ANcpLua.Agents.Testing.Workflows;

internal static class MessageDeliveryValidation
{
    public static void CheckDeliveries(this DeliveryMapping mapping, HashSet<string> receiverIds,
        HashSet<object> messages)
    {
        HashSet<string> unseenReceivers = [.. receiverIds];
        HashSet<object> unseenMessages = [.. messages];

        foreach (var grouping in mapping.Deliveries.GroupBy(static d => d.TargetId))
        {
            receiverIds.Should().Contain(grouping.Key);
            unseenReceivers.Remove(grouping.Key);

            foreach (var delivery in grouping)
            {
                // PortableValue.IsDelayedDeserialization and .Value are internal in MAF 1.3.0
                // (still internal as of the harvest; see ANcpLua.Agents/MAF1.3Mapping.md).
                // Simplified: just use the envelope message directly.
                var messageValue = delivery.Envelope.Message;

                messages.Should().Contain(messageValue);
                unseenMessages.Remove(messageValue);
            }
        }

        unseenReceivers.Should().BeEmpty();
        unseenMessages.Should().BeEmpty();
    }

    public static void CheckForwarded(Dictionary<string, List<MessageEnvelope>> queuedMessages,
        params (string expectedSender, List<string> expectedMessages)[] expectedForwards)
    {
        queuedMessages.Should().HaveCount(expectedForwards.Length);

        var perSenderValidations = expectedForwards.Select(forward => (Action<string>)(senderId =>
        {
            senderId.Should().Be(forward.expectedSender);
            queuedMessages[senderId].Should().HaveCount(forward.expectedMessages.Count);

            var validations = forward.expectedMessages
                .Select(m => (Action<MessageEnvelope>)(envelope => envelope.Message.Should().Be(m)))
                .ToArray();

            Assert.Collection(queuedMessages[senderId], validations);
        }));

        Assert.Collection(queuedMessages.Keys, perSenderValidations.ToArray());
    }
}