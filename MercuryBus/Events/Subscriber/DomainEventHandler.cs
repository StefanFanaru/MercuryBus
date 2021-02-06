using System;
using MercuryBus.Events.Common;
using MercuryBus.Messaging.Common;

namespace MercuryBus.Events.Subscriber
{
    public class DomainEventHandler
    {
        private readonly Action<IDomainEventEnvelope<IDomainEvent>, IServiceProvider> _handler;

        public DomainEventHandler(string aggregateType, Type eventType,
            Action<IDomainEventEnvelope<IDomainEvent>, IServiceProvider> handler)
        {
            AggregateType = aggregateType;
            EventType = eventType;
            _handler = handler;
        }

        public Type EventType { get; }
        public string AggregateType { get; }


        public bool Handles(IMessage message)
        {
            return AggregateType.Equals(message.GetRequiredHeader(EventMessageHeaders.AggregateType))
                   && string.Equals(EventType.Name, message.GetRequiredHeader(EventMessageHeaders.EventType));
        }

        public void Invoke(IDomainEventEnvelope<IDomainEvent> domainEventEnvelope, IServiceProvider serviceProvider)
        {
            _handler(domainEventEnvelope, serviceProvider);
        }
    }
}
