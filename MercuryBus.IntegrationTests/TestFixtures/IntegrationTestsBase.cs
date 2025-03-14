﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using MercuryBus.Database;
using MercuryBus.Events.Publisher;
using MercuryBus.IntegrationTests.TestHelpers;
using MercuryBus.Local.Kafka.Consumer;
using MercuryBus.Messaging.Common;
using MercuryBus.Messaging.Consumer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace MercuryBus.IntegrationTests.TestFixtures
{
    public class IntegrationTestsBase
    {
        private const string TestSettingsFile = "testsettings.json";
        private string _subscriberId = "xx";
        protected const string AggregateType12 = "TestMessage12Topic";
        protected const string AggregateType34 = "TestMessage34Topic";
        protected string MercuryDatabaseSchemaName = "mercury";

        protected TestSettings TestSettings;

        private static IHost _host;
        private static MercuryBusDbContext _dbContext;
        private static IDomainEventPublisher _domainEventPublisher;
        private static TestEventConsumer _testEventConsumer;
        private static TestMessageInterceptor _interceptor;

        public IntegrationTestsBase()
        {
            IConfigurationRoot configuration;
            try
            {
                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                ConfigureFromEnvironmentAndSettingsFile(configurationBuilder);
                configuration = configurationBuilder.Build();
            }
            catch
            {
                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                ConfigureFromEnvironment(configurationBuilder);
                configuration = configurationBuilder.Build();
            }

            TestSettings = configuration.Get<TestSettings>();
        }

        protected void TestSetup(string schema, bool withInterceptor,
            MercuryKafkaConsumerConfigurationProperties consumerConfigProperties)
        {
            MercuryDatabaseSchemaName = schema;
            _subscriberId = Guid.NewGuid().ToString();

            if (_host == null)
            {
                _host = SetupTestHost(withInterceptor, consumerConfigProperties);
                _dbContext = _host.Services.GetService<MercuryBusDbContext>();
                _domainEventPublisher = _host.Services.GetService<IDomainEventPublisher>();
                _testEventConsumer = _host.Services.GetService<TestEventConsumer>();
                _interceptor = (TestMessageInterceptor) _host.Services.GetService<IMessageInterceptor>();
            }
        }

        protected void CleanupTest()
        {
            ClearDb(GetDbContext(), MercuryDatabaseSchemaName);
            GetTestConsumer().Reset();
            GetTestMessageInterceptor()?.Reset();
        }

        protected async Task CleanupKafka()
        {
            var config = new AdminClientConfig();
            config.BootstrapServers = TestSettings.KafkaBootstrapServers;
            try
            {
                using (var admin = new AdminClientBuilder(config).Build())
                {
                    await admin.DeleteTopicsAsync(new[] {AggregateType12, AggregateType34});
                }
            }
            catch (DeleteTopicsException e)
            {
                // Don't fail if topic wasn't found (nothing to delete)
                if (e.Results.Where(r => r.Error.IsError).All(r => r.Error.Code == ErrorCode.UnknownTopicOrPart))
                {
                    TestContext.WriteLine(e.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        protected void ShowTestResults()
        {
            TestContext.WriteLine("Test Config");
            TestContext.WriteLine("  Connection String: {0}", TestSettings.ConnectionStrings.MercuryBusDbConnection);
            TestContext.WriteLine("  Kafka server:      {0}", TestSettings.KafkaBootstrapServers);
            TestContext.WriteLine("  Schema:            {0}", MercuryDatabaseSchemaName);
            TestContext.WriteLine("  Dispatcher Id:     {0}", _subscriberId);
            TestContext.WriteLine("  Aggregate Type 12: {0}", AggregateType12);
            TestContext.WriteLine("  Aggregate Type 34: {0}", AggregateType34);

            TestContext.WriteLine("Test Results");
            TestContext.WriteLine("  N Messages in DB:  {0}", _dbContext.Messages.Count());
            TestContext.WriteLine("  Unpublished Count: {0}", _dbContext.Messages.Count(msg => msg.Published == 0));
            TestContext.WriteLine("  N Received in DB:  {0}", _dbContext.ReceivedMessages.Count(msg => msg.MessageId != null));
            foreach (Type eventType in _testEventConsumer.GetEventTypes())
            {
                TestContext.WriteLine(
                    $"  Received {eventType.Name}   {_testEventConsumer.GetEventStatistics(eventType).MessageCount}");
            }

            TestContext.WriteLine("  Exception Count:   {0}", _testEventConsumer.ExceptionCount);

            if (_interceptor != null)
            {
                TestContext.WriteLine("Message Interceptor Counts");
                TestContext.WriteLine("  Pre Send:     {0}", _interceptor.PreSendCount);
                TestContext.WriteLine("  Post Send:    {0}", _interceptor.PostSendCount);
                TestContext.WriteLine("  Pre Receive:  {0}", _interceptor.PreReceiveCount);
                TestContext.WriteLine("  Post Receive: {0}", _interceptor.PostReceiveCount);
                TestContext.WriteLine("  Pre Handle:   {0}", _interceptor.PreHandleCount);
                TestContext.WriteLine("  Post Handle:  {0}", _interceptor.PostHandleCount);
            }
        }

        /// <summary>
        /// Set up the configuration for the HostBuilder
        /// </summary>
        protected void ConfigureFromEnvironmentAndSettingsFile(IConfigurationBuilder config,
            Dictionary<string, string> overrides = null)
        {
            config
                .AddJsonFile(TestSettingsFile, false)
                .AddEnvironmentVariables()
                .AddInMemoryCollection(overrides);
        }

        /// <summary>
        /// Set up the configuration for the HostBuilder
        /// </summary>
        protected void ConfigureFromEnvironment(IConfigurationBuilder config,
            Dictionary<string, string> overrides = null)
        {
            config
                .AddEnvironmentVariables()
                .AddInMemoryCollection(overrides);
        }

        protected IHost SetupTestHost(bool withInterceptor, MercuryKafkaConsumerConfigurationProperties consumerConfigProperties)
        {
            var host = new TestHostBuilder()
                .SetConnectionString(TestSettings.ConnectionStrings.MercuryBusDbConnection)
                .SetMercuryDatabaseSchemaName(MercuryDatabaseSchemaName)
                .SetKafkaBootstrapServers(TestSettings.KafkaBootstrapServers)
                .SetSubscriberId(_subscriberId)
                .SetDomainEventHandlersFactory(
                    provider =>
                    {
                        var consumer = provider.GetRequiredService<TestEventConsumer>();
                        return consumer.DomainEventHandlers(AggregateType12, AggregateType34);
                    })
                .SetConsumerConfigProperties(consumerConfigProperties)
                .Build<TestEventConsumer>(withInterceptor);
            host.StartAsync().Wait();
            return host;
        }

        protected void DisposeTestHost()
        {
            if (_host == null)
                return;

            var messageConsumer = _host.Services.GetService<IMessageConsumer>();
            messageConsumer.Close();
            _host.StopAsync().Wait();
            _host.Dispose();
            _host = null;
            _dbContext = null;
            _domainEventPublisher = null;
            _testEventConsumer = null;
        }

        protected TestEventConsumer GetTestConsumer()
        {
            return _testEventConsumer;
        }

        protected TestMessageInterceptor GetTestMessageInterceptor()
        {
            return _interceptor;
        }

        protected IDomainEventPublisher GetTestPublisher()
        {
            return _domainEventPublisher;
        }

        protected MercuryBusDbContext GetDbContext()
        {
            return _dbContext;
        }

        protected void ClearDb(MercuryBusDbContext dbContext, String mercuryDatabaseSchemaName)
        {
            dbContext.Database.ExecuteSqlRaw(String.Format("Delete from [{0}].[Messages]", mercuryDatabaseSchemaName));
            dbContext.Database.ExecuteSqlRaw(String.Format("Delete from [{0}].[ReceivedMessages]", mercuryDatabaseSchemaName));
        }
    }
}
