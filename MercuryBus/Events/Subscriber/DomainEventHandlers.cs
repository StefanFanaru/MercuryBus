using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MercuryBus.Messaging.Common;

namespace MercuryBus.Events.Subscriber
{
    public class DomainEventHandlers
    {
        public DomainEventHandlers(IList<DomainEventHandler> handlers)
        {
            Handlers = handlers;
        }

        public IList<DomainEventHandler> Handlers { get; }

        public ISet<string> GetAggregateTypes()
        {
            return Handlers.Select(x => x.AggregateType).ToImmutableHashSet();
        }

        public DomainEventHandler FindTargetMethod(IMessage message)
        {
            return Handlers.FirstOrDefault(x => x.Handles(message));
        }
    }
}