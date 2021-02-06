using System;

namespace MercuryBus.Consumer.Common
{
    public interface IMessageHandlerDecorator
    {
        Action<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain> Accept { get; }
    }
}