using System.Collections.Generic;

namespace MercuryBus.Local.Kafka.Consumer
{
    public class MercuryKafkaConsumerConfigurationProperties
    {
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        public static MercuryKafkaConsumerConfigurationProperties Empty()
        {
            return new();
        }
    }
}