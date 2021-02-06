using MercuryBus.Events.Common;
using MercuryBus.Messaging.Common;

namespace MercuryBus.Events.Subscriber
{
    public class DomainEventEnvelope<T> : IDomainEventEnvelope<T> where T : IDomainEvent
    {
        public DomainEventEnvelope(IMessage message, string aggregateType, string aggregateId, string eventId, T @event)
        {
            Message = message;
            AggregateType = aggregateType;
            AggregateId = aggregateId;
            EventId = eventId;
            Event = @event;
        }

        public string AggregateId { get; }

        public IMessage Message { get; }

        public T Event { get; }

        public string AggregateType { get; }

        public string EventId { get; }
    }
}