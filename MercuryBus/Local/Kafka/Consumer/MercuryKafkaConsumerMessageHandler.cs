using System;
using Confluent.Kafka;

namespace MercuryBus.Local.Kafka.Consumer
{
    public delegate void MercuryKafkaConsumerMessageHandler(ConsumeResult<string, string> consumeResult,
        Action<Exception> callback);
}