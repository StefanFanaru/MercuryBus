using MercuryBus.Events.Common;
using MercuryBus.Messaging.Common;

namespace MercuryBus.Events.Subscriber
{
    public interface IDomainEventEnvelope<out T> where T : IDomainEvent
    {
        string AggregateId { get; }
        IMessage Message { get; }
        string AggregateType { get; }
        string EventId { get; }

        T Event { get; }
    }
}