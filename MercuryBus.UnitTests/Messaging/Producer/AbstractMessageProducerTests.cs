using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MercuryBus.Messaging.Common;
using MercuryBus.Messaging.Producer;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace MercuryBus.UnitTests.Messaging.Producer
{
    public class AbstractMessageProducerTests
    {
        public class MyMessageProducer : AbstractMessageProducer
        {
            public MyMessageProducer(IEnumerable<IMessageInterceptor> messageInterceptors,
                ILogger<AbstractMessageProducer> logger)
                : base(logger, messageInterceptors)
            {
            }

            public void Send(string destination, IMessage message, IMessageSender ms)
            {
                SendMessage("id", destination, message, ms);
            }
        }

        [Test]
        public void Send_SimpleMessage_MessageHeadersAreApplied()
        {
            // Arrange
            Message sendMessage = null;

            var ms = Substitute.For<IMessageSender>();
            ms.Send(Arg.Do<Message>(arg => sendMessage = arg));

            // Act
            MyMessageProducer mp = new MyMessageProducer(new List<IMessageInterceptor>(),
                Substitute.For<ILogger<AbstractMessageProducer>>());
            mp.Send("Destination", MessageBuilder.WithPayload("x").Build(), ms);

            // Assert
            Assert.NotNull(sendMessage);
            Assert.NotNull(sendMessage.GetRequiredHeader(MessageHeaders.Id));
            Assert.NotNull(sendMessage.GetRequiredHeader(MessageHeaders.Destination));
            Assert.NotNull(sendMessage.GetRequiredHeader(MessageHeaders.Date));
        }
    }
}
