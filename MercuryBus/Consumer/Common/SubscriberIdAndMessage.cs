using MercuryBus.Messaging.Common;

namespace MercuryBus.Consumer.Common
{
    public class SubscriberIdAndMessage
    {
        public SubscriberIdAndMessage(string subscriberId, IMessage message)
        {
            SubscriberId = subscriberId;
            Message = message;
        }

        public string SubscriberId { get; }

        public IMessage Message { get; }
    }
}
