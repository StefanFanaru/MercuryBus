using System;
using System.Collections.Generic;
using System.Linq;
using MercuryBus.Messaging.Consumer;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Consumer.Common
{
    public class DecoratedMessageHandlerFactory
    {
        private readonly IList<IMessageHandlerDecorator> _decorators;
        private readonly ILogger<DecoratedMessageHandlerFactory> _logger;

        public DecoratedMessageHandlerFactory(ILogger<DecoratedMessageHandlerFactory> logger,
            IEnumerable<IMessageHandlerDecorator> decorators)
        {
            _logger = logger;
            _decorators = decorators.OrderBy(x => x is IOrdered ordered ? ordered.Order : int.MaxValue).ToList();
        }

        public Action<SubscriberIdAndMessage, IServiceProvider> Decorate(MessageHandler messageHandler)
        {
            var builder = MessageHandlerDecoratorChainBuilder.StartingWith(_decorators[0]);

            foreach (var messageHandlerDecorator in _decorators.Skip(1))
            {
                builder = builder.AndThen(messageHandlerDecorator);
            }

            var chain = builder.AndFinally((subscriberIdAndMessage, serviceProvider) =>
            {
                var subscriberId = subscriberIdAndMessage.SubscriberId;
                var message = subscriberIdAndMessage.Message;

                try
                {
                    _logger.LogDebug($"Invoking handler {subscriberId} {message.Id}");
                    messageHandler(subscriberIdAndMessage.Message, serviceProvider);
                    _logger.LogDebug($"Handled message {message.Id} for subscriberId='{subscriberId}'");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Got exception while handling message {message.Id} for subscriberId='{subscriberId}'");
                    throw;
                }
            });

            return (subscriberIdAndMessage, serviceProvider) => chain.InvokeNext(subscriberIdAndMessage, serviceProvider);
        }
    }
}