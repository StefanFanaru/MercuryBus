using System;
using MercuryBus.Events.Common;
using MercuryBus.Events.Subscriber;
using MercuryBus.Messaging.Common;
using NUnit.Framework;

namespace MercuryBus.IntegrationTests.TestHelpers
{
    public class TestMessageType2 : IDomainEvent
    {
        public String Name { get; set; }
        public int Value { get; set; }
        public TestMessageType2(String name, int value)
        {
            Name = name;
            Value = value;
        }

        public void AssertGoodMessageReceived(IDomainEventEnvelope<IDomainEvent> receivedMessage)
        {
            Assert.True(receivedMessage.Message.HasHeader(MessageHeaders.Id), "Message ID is in the header");
            TestMessageType2 @event = (TestMessageType2) receivedMessage.Event;
            Assert.AreEqual(Name, @event.Name, "Message Name is the same");
            Assert.AreEqual(Value, @event.Value, "Message Value is the same");
        }
    }
}
