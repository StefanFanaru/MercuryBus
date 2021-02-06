using System;
using System.Collections.Generic;
using System.Linq;
using MercuryBus.Messaging.Common;
using Microsoft.Extensions.DependencyInjection;

namespace MercuryBus.Consumer.Common
{
    public class PrePostHandlerMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
    {
        public Action<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain> Accept =>
            (subscriberIdAndMessage, serviceProvider, messageHandlerDecoratorChain) =>
            {
                var message = subscriberIdAndMessage.Message;
                var subscriberId = subscriberIdAndMessage.SubscriberId;
                var messageInterceptors = serviceProvider.GetServices<IMessageInterceptor>().ToArray();

                PreHandle(subscriberId, message, messageInterceptors);
                try
                {
                    messageHandlerDecoratorChain.InvokeNext(subscriberIdAndMessage, serviceProvider);
                    PostHandle(subscriberId, message, messageInterceptors, null);
                }
                catch (Exception e)
                {
                    PostHandle(subscriberId, message, messageInterceptors, e);
                    throw;
                }
            };

        public int Order => BuiltInMessageHandlerDecoratorOrder.PrePostHandlerMessageHandlerDecorator;

        private void PreHandle(string subscriberId, IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors)
        {
            foreach (var messageInterceptor in messageInterceptors)
            {
                messageInterceptor.PreHandle(subscriberId, message);
            }
        }

        private void PostHandle(string subscriberId, IMessage message,
            IEnumerable<IMessageInterceptor> messageInterceptors, Exception exception)
        {
            foreach (var messageInterceptor in messageInterceptors)
            {
                messageInterceptor.PostHandle(subscriberId, message, exception);
            }
        }
    }
}