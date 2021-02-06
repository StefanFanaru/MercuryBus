using System;
using System.Collections.Generic;
using System.Linq;
using MercuryBus.Messaging.Common;
using Microsoft.Extensions.DependencyInjection;

namespace MercuryBus.Consumer.Common
{
    public class PrePostReceiveMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
    {
        public Action<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain> Accept =>
            (subscriberIdAndMessage, serviceProvider, messageHandlerDecoratorChain) =>
            {
                var message = subscriberIdAndMessage.Message;
                var messageInterceptors = serviceProvider.GetServices<IMessageInterceptor>().ToArray();

                PreReceive(message, messageInterceptors);
                try
                {
                    messageHandlerDecoratorChain.InvokeNext(subscriberIdAndMessage, serviceProvider);
                }
                finally
                {
                    PostReceive(message, messageInterceptors);
                }
            };

        public int Order => BuiltInMessageHandlerDecoratorOrder.PrePostReceiveMessageHandlerDecorator;

        private void PreReceive(IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors)
        {
            foreach (var messageInterceptor in messageInterceptors)
            {
                messageInterceptor.PreReceive(message);
            }
        }


        private void PostReceive(IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors)
        {
            foreach (var messageInterceptor in messageInterceptors)
            {
                messageInterceptor.PostReceive(message);
            }
        }
    }
}