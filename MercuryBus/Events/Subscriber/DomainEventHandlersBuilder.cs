using System;
using System.Collections.Generic;
using MercuryBus.Events.Common;
using Microsoft.Extensions.DependencyInjection;

namespace MercuryBus.Events.Subscriber
{
    public class DomainEventHandlersBuilder
    {
        private readonly IList<DomainEventHandler> _handlers = new List<DomainEventHandler>();
        private string _aggregateType;

        public DomainEventHandlersBuilder(string aggregateType)
        {
            _aggregateType = aggregateType;
        }

        public static DomainEventHandlersBuilder ForAggregateType(string aggregateType)
        {
            return new(aggregateType);
        }

        public DomainEventHandlersBuilder OnEvent<TEvent>(Action<IDomainEventEnvelope<TEvent>> handler)
            where TEvent : IDomainEvent
        {
            _handlers.Add(new DomainEventHandler(_aggregateType, typeof(TEvent),
                (envelope, provider) => handler((IDomainEventEnvelope<TEvent>) envelope)));
            return this;
        }

        public DomainEventHandlersBuilder OnEvent<TEvent>(Action<IDomainEventEnvelope<TEvent>, IServiceProvider> handler)
            where TEvent : IDomainEvent
        {
            _handlers.Add(new DomainEventHandler(_aggregateType, typeof(TEvent),
                (envelope, provider) => handler((IDomainEventEnvelope<TEvent>) envelope, provider)));

            return this;
        }

        public DomainEventHandlersBuilder OnEvent<TEvent, TEventHandler>()
            where TEvent : IDomainEvent
            where TEventHandler : IDomainEventHandler<TEvent>
        {
            _handlers.Add(new DomainEventHandler(_aggregateType, typeof(TEvent), (envelope, provider) =>
            {
                var eventHandler = provider.GetRequiredService<TEventHandler>();
                eventHandler.Handle((IDomainEventEnvelope<TEvent>) envelope);
            }));
            return this;
        }

        public DomainEventHandlersBuilder AndForAggregateType(string aggregateType)
        {
            _aggregateType = aggregateType;
            return this;
        }

        public DomainEventHandlers Build()
        {
            return new(_handlers);
        }
    }
}
