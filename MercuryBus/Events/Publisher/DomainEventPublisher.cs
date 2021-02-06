using System.Collections.Generic;
using System.Threading.Tasks;
using MercuryBus.Events.Common;
using MercuryBus.Helpers;
using MercuryBus.Messaging.Common;
using MercuryBus.Messaging.Producer;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Events.Publisher
{
    public class DomainEventPublisher : IDomainEventPublisher
    {
        private readonly ILogger<DomainEventPublisher> _logger;
        private readonly IMessageProducer _messageProducer;

        public DomainEventPublisher(ILogger<DomainEventPublisher> logger, IMessageProducer messageProducer)
        {
            _logger = logger;
            _messageProducer = messageProducer;
        }

        public void Publish(string aggregateType, object aggregateId, IList<IDomainEvent> domainEvents)
        {
            Publish(aggregateType, aggregateId, new Dictionary<string, string>(), domainEvents);
        }

        public void Publish(string aggregateType, object aggregateId, IDictionary<string, string> extraHeaders,
            IList<IDomainEvent> domainEvents)
        {
            var logContext = $"{nameof(Publish)}, aggregateType='{aggregateType}', aggregateId='{aggregateId}'" +
                             $"with {extraHeaders.Count} headers and {domainEvents.Count} events";

            _logger.LogDebug($"+{logContext}");

            foreach (var domainEvent in domainEvents)
            {
                _messageProducer.Send(aggregateType,
                    MakeMessageForDomainEvent(aggregateType, aggregateId, extraHeaders, domainEvent));
            }
        }

        public void Publish<TAggregate>(object aggregateId, IList<IDomainEvent> domainEvents)
        {
            Publish(typeof(TAggregate).FullName, aggregateId, domainEvents);
        }

        public async Task PublishAsync(string aggregateType, object aggregateId, IDictionary<string, string> extraHeaders,
            IList<IDomainEvent> domainEvents)
        {
            var logContext = $"{nameof(PublishAsync)}, aggregateType='{aggregateType}', aggregateId='{aggregateId}'" +
                             $"with {extraHeaders.Count} headers and {domainEvents.Count} events";
            _logger.LogDebug($"+{logContext}");

            foreach (var domainEvent in domainEvents)
            {
                await _messageProducer.SendAsync(aggregateType,
                    MakeMessageForDomainEvent(aggregateType, aggregateId, extraHeaders, domainEvent));
            }

            _logger.LogDebug($"-{logContext}");
        }

        public async Task PublishAsync(string aggregateType, object aggregateId, IList<IDomainEvent> domainEvents)
        {
            await PublishAsync(aggregateType, aggregateId, new Dictionary<string, string>(), domainEvents);
        }

        public async Task PublishAsync<TAggregate>(object aggregateId, IList<IDomainEvent> domainEvents)
        {
            await PublishAsync(typeof(TAggregate).FullName, aggregateId, domainEvents);
        }

        public static IMessage MakeMessageForDomainEvent(string aggregateType, object aggregateId,
            IDictionary<string, string> extraHeaders, IDomainEvent domainEvent)
        {
            var aggregateIdString = aggregateId.ToString();
            var evenType = domainEvent.GetType().Name;

            return MessageBuilder
                .WithPayload(domainEvent.ToJson())
                .WithExtraHeaders("", extraHeaders)
                .WithHeader(MessageHeaders.PartitionId, aggregateIdString)
                .WithHeader(EventMessageHeaders.AggregateId, aggregateIdString)
                .WithHeader(EventMessageHeaders.AggregateType, aggregateType)
                .WithHeader(EventMessageHeaders.EventType, evenType)
                .Build();
        }
    }
}