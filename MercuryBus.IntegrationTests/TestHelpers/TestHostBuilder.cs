using System;
using MercuryBus.Database;
using MercuryBus.Events.Subscriber;
using MercuryBus.Helpers;
using MercuryBus.Local.Kafka.Consumer;
using MercuryBus.Messaging.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MercuryBus.IntegrationTests.TestHelpers
{
    public class TestHostBuilder
    {
        private String _sqlConnectionString;
        private String _mercuryDatabaseSchemaName;
        private String _kafkaBootstrapServers;
        private String _subscriberId;
        private Func<IServiceProvider, DomainEventHandlers> _domainEventHandlersFactory;
        private MercuryKafkaConsumerConfigurationProperties _consumerConfigProperties = MercuryKafkaConsumerConfigurationProperties.Empty();

        private IHost _host;

        public TestHostBuilder SetConnectionString(String sqlConnectionString)
        {
            _sqlConnectionString = sqlConnectionString;
            return this;
        }

        public TestHostBuilder SetMercuryDatabaseSchemaName(String mercuryDatabaseSchemaName)
        {
            _mercuryDatabaseSchemaName = mercuryDatabaseSchemaName;
            return this;
        }

        public TestHostBuilder SetKafkaBootstrapServers(String kafkaBootstrapServers)
        {
            _kafkaBootstrapServers = kafkaBootstrapServers;
            return this;
        }

        public TestHostBuilder SetSubscriberId(String subscriberId)
        {
            _subscriberId = subscriberId;
            return this;
        }

        public TestHostBuilder SetDomainEventHandlersFactory(
            Func<IServiceProvider, DomainEventHandlers> domainEventHandlersFactory)
        {
            _domainEventHandlersFactory = domainEventHandlersFactory;
            return this;
        }

        public TestHostBuilder SetConsumerConfigProperties(MercuryKafkaConsumerConfigurationProperties consumerConfigProperties)
        {
            _consumerConfigProperties = consumerConfigProperties;
            return this;
        }


        public IHost Build<TConsumerType>(bool withInterceptor) where TConsumerType : class
        {
            _host = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<MercuryBusDbContext>((provider, o) =>
                    {
                        o.UseSqlServer(_sqlConnectionString)
                            // Use a model cache key factory that ensures a new model is created if MercurySchema is changed
                            .ReplaceService<IModelCacheKeyFactory, DynamicMercurySchemaModelCacheKeyFactory>();
                    });
                    services.AddMercuryBusSqlKafkaTransport(_mercuryDatabaseSchemaName, _kafkaBootstrapServers, MercuryKafkaConsumerConfigurationProperties.Empty(),
                        (provider, o) =>
                        {
                            o.UseSqlServer(_sqlConnectionString);
                        });
                    if (withInterceptor)
                    {
                        services.AddSingleton<IMessageInterceptor>(new TestMessageInterceptor());
                    }

                    // Publisher Setup
                    services.AddMercuryBusEventsPublisher();

                    // Consumer Setup
                    services.AddSingleton<TConsumerType>();
                    services.AddMercuryBusDomainEventDispatcher(_subscriberId, _domainEventHandlersFactory);
                    services.AddSingleton<TestMessage4Handler>();
                })
                .Build();
            return _host;
        }
    }
}
