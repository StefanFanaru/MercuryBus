using System.Collections.Generic;
using System.Threading.Tasks;
using MercuryBus.Events.Common;

namespace MercuryBus.Events.Publisher
{
    public interface IDomainEventPublisher
    {
        void Publish(string aggregateType, object aggregateId, IList<IDomainEvent> domainEvents);

        void Publish(string aggregateType, object aggregateId, IDictionary<string, string> extraHeaders,
            IList<IDomainEvent> domainEvents);

        void Publish<TAggregate>(object aggregateId, IList<IDomainEvent> domainEvents);

        Task PublishAsync(string aggregateType, object aggregateId, IDictionary<string, string> extraHeaders,
            IList<IDomainEvent> domainEvents);

        Task PublishAsync(string aggregateType, object aggregateId, IList<IDomainEvent> domainEvents);
        Task PublishAsync(string aggregateType, object aggregateId, IDomainEvent domainEvent);
        Task PublishAsync<TAggregate>(object aggregateId, IList<IDomainEvent> domainEvents);
        Task PublishAsync<TAggregate>(object aggregateId, IDomainEvent domainEvent);
    }
}