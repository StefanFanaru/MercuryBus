
using System;

namespace MercuryBus.Consumer.Common
{
    public interface IDuplicateMessageDetector
    {
        bool IsDuplicate(string consumerId, string messageId);

        void DoWithMessage(SubscriberIdAndMessage subscriberIdAndMessage, Action callback);
    }
}
