using System.Collections.Generic;
using MercuryBus.Messaging.Common;

namespace MercuryBus.Messaging.Producer
{
    public class MessageBuilder
    {
        protected readonly string Body;
        protected readonly IDictionary<string, string> Headers = new Dictionary<string, string>();

        protected MessageBuilder()
        {
        }

        public MessageBuilder(string body)
        {
            Body = body;
        }

        public MessageBuilder(IMessage message) : this(message.Payload)
        {
            Headers = message.Headers;
        }

        public static MessageBuilder WithPayload(string payload)
        {
            return new(payload);
        }

        public MessageBuilder WithHeader(string name, string value)
        {
            Headers[name] = value;
            return this;
        }

        public MessageBuilder WithExtraHeaders(string prefix, IDictionary<string, string> headers)
        {
            foreach (var (key, value) in headers)
            {
                Headers[prefix + key] = value;
            }

            return this;
        }

        public Message Build()
        {
            return new(Body, Headers);
        }

        public static MessageBuilder WithMessage(Message message)
        {
            return new(message);
        }
    }
}