namespace MercuryBus.Database
{
    public class MercuryBusSchema
    {
        public const string DefaultSchema = "mercury";

        public MercuryBusSchema()
        {
            MessageBusDatabaseSchema = DefaultSchema;
        }

        public MercuryBusSchema(string messageBusDatabaseSchema)
        {
            MessageBusDatabaseSchema =
                string.IsNullOrWhiteSpace(messageBusDatabaseSchema) ? DefaultSchema : messageBusDatabaseSchema;
        }

        public string MessageBusDatabaseSchema { get; }
    }
}
