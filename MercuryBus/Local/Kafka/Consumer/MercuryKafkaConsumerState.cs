namespace MercuryBus.Local.Kafka.Consumer
{
    public enum MercuryKafkaConsumerState
    {
        MessageHandlingFailed,
        Started,
        FailedToStart,
        Stopped,
        Failed,
        Created
    }
}