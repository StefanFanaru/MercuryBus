using System;
using MercuryBus.Consumer.Common;
using MercuryBus.Consumer.Database;
using MercuryBus.Consumer.Kafka;
using MercuryBus.Database;
using MercuryBus.Events.Publisher;
using MercuryBus.Events.Subscriber;
using MercuryBus.Local.Kafka.Consumer;
using MercuryBus.Messaging.Consumer;
using MercuryBus.Messaging.Producer;
using MercuryBus.Messaging.Producer.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Helpers
{
    public static class MercuryServiceCollectionExtensions
    {
        public static void AddMercuryBusSqlKafkaTransport(this IServiceCollection serviceCollection,
            string mercuryDatabaseSchema, string bootstrapServers,
            MercuryKafkaConsumerConfigurationProperties consumerConfigurationProperties,
            Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
        {
            AddMercuryBusSqlProducer(serviceCollection, mercuryDatabaseSchema, dbContextOptionsAction);
            AddMercuryBusKafkaConsumer(serviceCollection, mercuryDatabaseSchema, bootstrapServers,
                consumerConfigurationProperties, dbContextOptionsAction);
        }

        public static void AddMercuryBusEventsPublisher(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddScoped<IDomainEventPublisher, DomainEventPublisher>();
        }

        public static void AddMercuryBusDomainEventDispatcher(
            this IServiceCollection serviceCollection, string subscriberId,
            Func<IServiceProvider, DomainEventHandlers> domainEventHandlersFactory)
        {
            serviceCollection.AddSingleton(provider =>
            {
                var messageConsumer = provider.GetRequiredService<IMessageConsumer>();
                var logger = provider.GetRequiredService<ILogger<DomainEventDispatcher>>();

                var dispatcher = new DomainEventDispatcher(subscriberId, logger,
                    messageConsumer, domainEventHandlersFactory(provider));

                return dispatcher;
            });
        }

        public static void AddMercuryBusSqlProducer(this IServiceCollection serviceCollection,
            string mercuryDatabaseSchema, Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
        {
            AddMercuryBusCommonSqlMessagingServices(serviceCollection, mercuryDatabaseSchema, dbContextOptionsAction);
            serviceCollection.TryAddSingleton<IIdGenerator, IdGenerator>();
            serviceCollection.TryAddSingleton<ITimingProvider, TimingProvider>();
            serviceCollection.TryAddScoped<IMessageProducer, DatabaseMessageProducer>();
        }

        private static void AddMercuryBusCommonSqlMessagingServices(
            this IServiceCollection serviceCollection, string mercuryDatabaseSchema,
            Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
        {
            serviceCollection.TryAddSingleton(provider => new MercuryBusSchema(mercuryDatabaseSchema));
            serviceCollection.AddDbContext<MercuryBusDbContext>(dbContextOptionsAction);
            serviceCollection.TryAddScoped<IMercuryBusDbContextProvider, MercuryBusDbContextProvider>();
        }

        public static void AddMercuryBusKafkaConsumer(this IServiceCollection serviceCollection,
            string mercuryDatabaseSchema, string bootstrapServers,
            MercuryKafkaConsumerConfigurationProperties consumerConfigurationProperties,
            Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
        {
            AddMercuryBusCommonSqlMessagingServices(serviceCollection, mercuryDatabaseSchema, dbContextOptionsAction);
            AddMercuryBusCommonConsumer(serviceCollection);
            serviceCollection.TryAddScoped<IDuplicateMessageDetector, SqlTableBasedDuplicateMessageDetector>();
            serviceCollection.TryAddSingleton(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
                var decoratedMessageHandlerFactory = provider.GetRequiredService<DecoratedMessageHandlerFactory>();

                IMessageConsumer messageConsumer = new KafkaMessageConsumer(bootstrapServers,
                    decoratedMessageHandlerFactory, loggerFactory,
                    serviceScopeFactory, consumerConfigurationProperties);

                return messageConsumer;
            });
            serviceCollection.AddHostedService<DomainEventDispatcherInitializer>();
        }

        private static void AddMercuryBusCommonConsumer(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<DecoratedMessageHandlerFactory>();
            serviceCollection.AddSingleton<IMessageHandlerDecorator, PrePostReceiveMessageHandlerDecorator>();
            serviceCollection.AddSingleton<IMessageHandlerDecorator, DuplicateDetectingMessageHandlerDecorator>();
            serviceCollection.AddSingleton<IMessageHandlerDecorator, PrePostHandlerMessageHandlerDecorator>();
        }
    }
}