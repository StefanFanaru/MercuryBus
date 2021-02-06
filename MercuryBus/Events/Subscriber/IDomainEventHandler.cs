using MercuryBus.Events.Common;

namespace MercuryBus.Events.Subscriber
{
    public interface IDomainEventHandler<in T> where T : IDomainEvent
    {
        void Handle(IDomainEventEnvelope<T> @event);
    }
}