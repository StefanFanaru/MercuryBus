using System;
using System.Collections.Generic;
using Confluent.Kafka;
using MercuryBus.Consumer.Common;
using MercuryBus.Helpers;
using MercuryBus.Local.Kafka.Consumer;
using MercuryBus.Messaging.Common;
using MercuryBus.Messaging.Consumer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Consumer.Kafka
{
    public sealed class KafkaMessageConsumer : IMessageConsumer, IDisposable
    {
        private readonly string _boostrapServers;
        private readonly List<MercuryKafkaConsumer> _consumers = new();
        private readonly DecoratedMessageHandlerFactory _decoratedMessageHandlerFactory;
        private readonly ILogger<KafkaMessageConsumer> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MercuryKafkaConsumerConfigurationProperties _mercuryKafkaConsumerConfiguration;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly string _id = Guid.NewGuid().ToString();

        public KafkaMessageConsumer(string boostrapServers,
            DecoratedMessageHandlerFactory decoratedMessageHandlerFactory,
            ILoggerFactory loggerFactory,
            IServiceScopeFactory serviceScopeFactory,
            MercuryKafkaConsumerConfigurationProperties mercuryKafkaConsumerConfiguration)
        {
            _mercuryKafkaConsumerConfiguration = mercuryKafkaConsumerConfiguration;
            _loggerFactory = loggerFactory;
            _serviceScopeFactory = serviceScopeFactory;
            _boostrapServers = boostrapServers;
            _decoratedMessageHandlerFactory = decoratedMessageHandlerFactory;
            _logger = _loggerFactory.CreateLogger<KafkaMessageConsumer>();
        }

        public void Dispose()
        {
            Close();
        }

        public IMessageSubscription Subscribe(string subscriberId, ISet<string> channels, MessageHandler handler)
        {
            var logContext = $"{nameof(Subscribe)} for subscriberId='{subscriberId}', " +
                             $"channels='{string.Join(",", channels)}', " +
                             $"handler='{handler.Method.Name}'";
            _logger.LogDebug($"+{logContext}");

            var decoratedHandler = _decoratedMessageHandlerFactory.Decorate(handler);
            var swimlaneBaseDispatcher = new SwimlaneBasedDispatcher(subscriberId, _loggerFactory);
            MercuryKafkaConsumerMessageHandler kafkaConsumerHandler = (record, completionCallback) =>
                swimlaneBaseDispatcher.Dispatch(ToMessage(record), record.Partition,
                    message => Handle(message, completionCallback, subscriberId, decoratedHandler));

            var kafkaConsumer = new MercuryKafkaConsumer(subscriberId, kafkaConsumerHandler,
                new List<string>(channels),
                _boostrapServers,
                _mercuryKafkaConsumerConfiguration,
                _loggerFactory);

            _consumers.Add(kafkaConsumer);

            kafkaConsumer.Start();

            _logger.LogDebug($"-{logContext}");
            return new MessageSubscription(() =>
            {
                kafkaConsumer.Dispose();
                _consumers.Remove(kafkaConsumer);
            });
        }

        public string GetId()
        {
            return _id;
        }

        public void Close()
        {
            _logger.LogDebug($"{nameof(Close)}");
            foreach (var consumer in _consumers)
            {
                consumer.Dispose();
            }

            _consumers.Clear();
            _logger.LogDebug($"-{nameof(Close)}");
        }

        public void Handle(IMessage message, Action<Exception> completionCallback, string subscriberId,
            Action<SubscriberIdAndMessage, IServiceProvider> decoratedHandler)
        {
            try
            {
                // Creating a service scope and passing it to handlers
                // so they can resolve scoped services
                using var scope = _serviceScopeFactory.CreateScope();
                decoratedHandler(new SubscriberIdAndMessage(subscriberId, message), scope.ServiceProvider);
                completionCallback(null);
            }
            catch (Exception e)
            {
                completionCallback(e);
                throw;
            }
        }

        private IMessage ToMessage(ConsumeResult<string, string> record)
        {
            return record.Message.Value.FromJson<Message>();
        }

        private class MessageSubscription : IMessageSubscription
        {
            private readonly Action _unsubscribe;

            public MessageSubscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Unsubscribe()
            {
                _unsubscribe();
            }
        }
    }
}
