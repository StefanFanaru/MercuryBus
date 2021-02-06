
namespace MercuryBus.Consumer.Common
{
    public static class BuiltInMessageHandlerDecoratorOrder
    {
        public const int PrePostReceiveMessageHandlerDecorator = 100;
        public const int DuplicateDetectingMessageHandlerDecorator = 200;
        public const int PrePostHandlerMessageHandlerDecorator = 300;
    }
}
