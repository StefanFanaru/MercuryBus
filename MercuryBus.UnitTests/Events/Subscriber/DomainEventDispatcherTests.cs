using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MercuryBus.Events.Common;
using MercuryBus.Events.Publisher;
using MercuryBus.Events.Subscriber;
using MercuryBus.Messaging.Common;
using MercuryBus.Messaging.Consumer;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace MercuryBus.UnitTests.Events.Subscriber
{
    public class DomainEventDispatcherTests
    {
        private string _subscriberId = "test-subscriber-id";
        private static string aggregateType = "AggregateType";

        private string aggregateId = "xyz";
        private string messageId = "message-" + DateTime.Now;

        public class MyTarget
        {
            public ConcurrentQueue<IDomainEventEnvelope<IDomainEvent>> Queue = new ConcurrentQueue<IDomainEventEnvelope<IDomainEvent>>();

            public DomainEventHandlers DomainEventHandlers()
            {
                return DomainEventHandlersBuilder
                    .ForAggregateType(aggregateType)
                    .OnEvent<MyDomainEvent>(HandleAccountDebited)
                    .Build();
            }

            internal void HandleAccountDebited(IDomainEventEnvelope<MyDomainEvent> message)
            {
                Queue.Enqueue(message);
            }

        }

        public class MyDomainEvent : IDomainEvent
        {
        }

        [Test]
        public void MessageHandler_ValidMessage_RegisteredHandlerCalled()
        {
			// Arrange
            MyTarget target = new MyTarget();

            var messageConsumer = Substitute.For<IMessageConsumer>();
            var serviceProvider = Substitute.For<IServiceProvider>();
	        var logger = Substitute.For<ILogger<DomainEventDispatcher>>();

            var dispatcher = new DomainEventDispatcher(
	            _subscriberId, logger, messageConsumer, target.DomainEventHandlers());

            dispatcher.Initialize();

			// Act
            dispatcher.MessageHandler(DomainEventPublisher.MakeMessageForDomainEvent(aggregateType,
                aggregateId, new Dictionary<string, string> {{ MessageHeaders.Id, messageId } },
                new MyDomainEvent()), serviceProvider);

			// Assert
            Assert.True(target.Queue.TryPeek(out var dee));
            Assert.NotNull(dee);
            Assert.AreEqual(aggregateId, dee.AggregateId);
            Assert.AreEqual(aggregateType, dee.AggregateType);
            Assert.AreEqual(messageId, dee.EventId);
        }
    }
}
