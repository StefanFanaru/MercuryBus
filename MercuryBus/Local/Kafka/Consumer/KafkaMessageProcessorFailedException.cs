using System;

namespace MercuryBus.Local.Kafka.Consumer
{
    public class KafkaMessageProcessorFailedException : Exception
    {
        public KafkaMessageProcessorFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}