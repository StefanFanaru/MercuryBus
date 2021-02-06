using System;
using MercuryBus.Events.Common;
using MercuryBus.Helpers;
using MercuryBus.Messaging.Common;
using MercuryBus.Messaging.Consumer;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Events.Subscriber
{
    public class DomainEventDispatcher
    {
        private readonly string _dispatcherContext;
        private readonly DomainEventHandlers _domainEventHandlers;
        private readonly ILogger<DomainEventDispatcher> _logger;
        private readonly IMessageConsumer _messageConsumer;
        private readonly string _subscriberId;

        public DomainEventDispatcher(string subscriberId,
            ILogger<DomainEventDispatcher> logger,
            IMessageConsumer messageConsumer,
            DomainEventHandlers domainEventHandlers)
        {
            _logger = logger;
            _subscriberId = subscriberId;
            _domainEventHandlers = domainEventHandlers;
            _messageConsumer = messageConsumer;
            _dispatcherContext = $"SubscriberId='{subscriberId}', " +
                                 $"DomainEventHandler for '{string.Join(',', domainEventHandlers.GetAggregateTypes())}'";
        }

        public void Initialize()
        {
            _messageConsumer.Subscribe(_subscriberId, _domainEventHandlers.GetAggregateTypes(), MessageHandler);
        }

        public void MessageHandler(IMessage message, IServiceProvider serviceProvider)
        {
            var logContext = $"{nameof(MessageHandler)} for {_dispatcherContext}, MessageId={message.Id}";
            _logger.LogDebug($"+{logContext}");
            var aggregateType = message.GetRequiredHeader(EventMessageHeaders.AggregateType);

            var handler = _domainEventHandlers.FindTargetMethod(message);

            if (handler == null)
            {
                _logger.LogDebug($"{logContext}: No handler found for type='{aggregateType}'");
                return;
            }

            var param = (IDomainEvent) message.Payload.FromJson(handler.EventType);
            var envelopeType = typeof(DomainEventEnvelope<>).MakeGenericType(handler.EventType);
            var envelope = (IDomainEventEnvelope<IDomainEvent>) Activator.CreateInstance(envelopeType,
                message,
                aggregateType,
                message.GetRequiredHeader(EventMessageHeaders.AggregateId),
                message.GetRequiredHeader(MessageHeaders.Id),
                param);

            handler.Invoke(envelope, serviceProvider);
            _logger.LogDebug($"-{logContext}: Processed message of type'{aggregateType}'");
        }
    }
}