using System;
using System.Collections.Generic;

namespace MercuryBus.Consumer.Common
{
    public class MessageHandlerDecoratorChainBuilder
    {
        private readonly LinkedList<IMessageHandlerDecorator> _handlers = new();

        public static MessageHandlerDecoratorChainBuilder StartingWith(IMessageHandlerDecorator handlerDecorator)
        {
            var builder = new MessageHandlerDecoratorChainBuilder();
            builder.Add(handlerDecorator);
            return builder;
        }


        public MessageHandlerDecoratorChainBuilder AndThen(IMessageHandlerDecorator handlerDecorator)
        {
            Add(handlerDecorator);
            return this;
        }

        public IMessageHandlerDecoratorChain AndFinally(Action<SubscriberIdAndMessage, IServiceProvider> consumer)
        {
            return BuildChain(_handlers.First, consumer);
        }

        private void Add(IMessageHandlerDecorator handlerDecorator)
        {
            _handlers.AddLast(handlerDecorator);
        }

        private static IMessageHandlerDecoratorChain BuildChain(LinkedListNode<IMessageHandlerDecorator> handlersHead,
            Action<SubscriberIdAndMessage, IServiceProvider> consumer)
        {
            if (handlersHead == null)
            {
                return new MessageHandlerDecoratorChain(consumer);
            }

            var tail = handlersHead.Next;
            return new MessageHandlerDecoratorChain((subscriberIdAndMessage, provider) =>
                handlersHead.Value.Accept(subscriberIdAndMessage, provider, BuildChain(tail, consumer)));
        }

        private class MessageHandlerDecoratorChain : IMessageHandlerDecoratorChain
        {
            private readonly Action<SubscriberIdAndMessage, IServiceProvider> _action;

            public MessageHandlerDecoratorChain(Action<SubscriberIdAndMessage, IServiceProvider> action)
            {
                _action = action;
            }

            public void InvokeNext(SubscriberIdAndMessage subscriberIdAndMessage, IServiceProvider serviceProvider)
            {
                _action(subscriberIdAndMessage, serviceProvider);
            }
        }
    }
}