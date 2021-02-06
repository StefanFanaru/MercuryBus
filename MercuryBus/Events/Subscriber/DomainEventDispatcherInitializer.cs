using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MercuryBus.Events.Subscriber
{
    /// <summary>
    ///     Responsible for initializing all the DomainEventDispatcher instances on service startup.
    /// </summary>
    public class DomainEventDispatcherInitializer : IHostedService
    {
        private readonly IEnumerable<DomainEventDispatcher> _domainEventDispatchers;

        public DomainEventDispatcherInitializer(IEnumerable<DomainEventDispatcher> domainEventDispatchers)
        {
            _domainEventDispatchers = domainEventDispatchers;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var domainEventDispatcher in _domainEventDispatchers)
            {
                domainEventDispatcher.Initialize();
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}