using MercuryBus.Consumer.Database;
using MercuryBus.Messaging.Producer.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MercuryBus.Database
{
    public class MercuryBusDbContext : DbContext
    {
        // Note that this is MSSQL specific. It is only used if an entity framework migration is generated for this DbContext
        private const string CurrentTimeInMillisecondSql = "DATEDIFF_BIG(ms, '1970-01-01 00:00:00', GETUTCDATE())";
        private readonly MercuryBusSchema _mercuryBusSchema;

        public MercuryBusDbContext()
        {
        }

        public MercuryBusDbContext(DbContextOptions<MercuryBusDbContext> options, MercuryBusSchema schema) : base(options)
        {
            _mercuryBusSchema = schema;
        }

        public string MercuryBusDatabaseSchema => _mercuryBusSchema.MessageBusDatabaseSchema;

        /// <summary>
        ///     Table to hold published messages that get sent to the messaging system
        /// </summary>
        public DbSet<Message> Messages { get; set; }

        /// <summary>
        ///     Table to track which messages have been processed for subscribers
        /// </summary>
        public DbSet<ReceivedMessage> ReceivedMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(MercuryBusDatabaseSchema);
            modelBuilder.Entity<Message>(ConfigureMessage);
            modelBuilder.Entity<ReceivedMessage>(ConfigureReceivedMessage);
        }

        private void ConfigureReceivedMessage(EntityTypeBuilder<ReceivedMessage> builder)
        {
            builder.ToTable("ReceivedMessages");
            builder.HasKey(x => new {x.ConsumerId, x.MessageId});
            builder.Property(x => x.ConsumerId).HasColumnType("varchar(450)").IsRequired();
            builder.Property(x => x.MessageId).HasColumnType("varchar(450)").IsRequired();
            builder.Property(x => x.CreationTime).HasDefaultValueSql(CurrentTimeInMillisecondSql).IsRequired(false);
        }

        private void ConfigureMessage(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnType("varchar(450)");
            builder.Property(x => x.Destination).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.Headers).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.Payload).IsRequired();
            builder.Property(x => x.Published).HasDefaultValue((short?) 0).IsRequired(false);
            builder.Property(x => x.CreationTime).HasDefaultValueSql(CurrentTimeInMillisecondSql).IsRequired(false);

            builder.HasIndex(x => new {x.Published, x.Id}).HasName("Message_Published_IDX");
        }
    }
}
