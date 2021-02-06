using System;

namespace MercuryBus.Consumer.Common
{
    public interface IMessageHandlerDecoratorChain
    {
        void InvokeNext(SubscriberIdAndMessage subscriberIdAndMessage, IServiceProvider serviceProvider);
    }
}