﻿using System;
using MercuryBus.Events.Subscriber;
using Microsoft.Extensions.Logging;

namespace MercuryBus.IntegrationTests.TestHelpers
{
	public class TestMessage4Handler : IDomainEventHandler<TestMessageType4>
	{
		private readonly ILogger _logger;
		private TestEventConsumer TestEventConsumer { get; }

		public TestMessage4Handler(TestEventConsumer testEventConsumer,
			ILogger<TestMessage4Handler> logger)
		{
			TestEventConsumer = testEventConsumer;
			_logger = logger;
		}

		public void Handle(IDomainEventEnvelope<TestMessageType4> @event)
		{
			_logger.LogDebug("Got message MessageType4Event with id={} and value={}", @event.EventId,
				@event.Event.ToString());
			TestEventConsumer.EventStatistics eventStatistics = TestEventConsumer.GetEventStatistics(typeof(TestMessageType4));
			TestEventConsumer.HandleTestMessageEvent(@event, eventStatistics);
		}
	}

}
