using MercuryBus.Helpers;
using MercuryBus.Messaging.Common;

namespace MercuryBus.Messaging.Producer.Database
{
    public class Message
    {
        public Message()
        {
        }

        public Message(IMessage message)
        {
            Id = message.Id;
            Destination = message.GetRequiredHeader(MessageHeaders.Destination);
            Headers = message.Headers.ToJson();
            Payload = message.Payload;
        }

        public string Id { get; set; }
        public string Destination { get; set; }
        public string Headers { get; set; }
        public string Payload { get; set; }
        public short? Published { get; set; }
        public long? CreationTime { get; set; }
    }
}