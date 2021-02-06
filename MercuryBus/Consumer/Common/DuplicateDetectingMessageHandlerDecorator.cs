using System;
using Microsoft.Extensions.DependencyInjection;

namespace MercuryBus.Consumer.Common
{
    public class DuplicateDetectingMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
    {
        public Action<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain> Accept =>
            (subscriberIdAndMessage, serviceProvider, messageHandlerDecoratorChain) =>
            {
                var duplicateMessageDetector = serviceProvider.GetRequiredService<IDuplicateMessageDetector>();
                duplicateMessageDetector.DoWithMessage(subscriberIdAndMessage,
                    () => messageHandlerDecoratorChain.InvokeNext(subscriberIdAndMessage, serviceProvider));
            };

        public int Order => BuiltInMessageHandlerDecoratorOrder.DuplicateDetectingMessageHandlerDecorator;
    }
}