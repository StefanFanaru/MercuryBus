using System.Collections.Generic;
using System.Threading.Tasks;
using MercuryBus.Database;
using MercuryBus.Helpers;
using MercuryBus.Messaging.Common;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Messaging.Producer.Database
{
    /// <summary>
    ///     Uses a database context to insert messages in an outbox table to be sent to the message queue.
    ///     External CDC service polls the outbox table and puts the messages in the message queue.
    /// </summary>
    public class DatabaseMessageProducer : AbstractMessageProducer, IMessageProducer, IMessageSender
    {
        private readonly IMercuryBusDbContextProvider _dbContextProvider;
        private readonly IIdGenerator _idGenerator;

        /// <summary>
        ///     Construct an OutboxMessageProducer
        /// </summary>
        /// <param name="messageInterceptors">
        ///     Collection of intercepts applied before and
        ///     after sending the message to outbox
        /// </param>
        /// <param name="idGenerator">Function to use for generating keys</param>
        /// <param name="dbContextProvider">DbContext provider used to provide persistence to the outbox</param>
        /// <param name="logger">Logger for diagnosing messages</param>
        public DatabaseMessageProducer(ILogger<DatabaseMessageProducer> logger,
            IEnumerable<IMessageInterceptor> messageInterceptors,
            IMercuryBusDbContextProvider dbContextProvider,
            IIdGenerator idGenerator) : base(logger, messageInterceptors)
        {
            _dbContextProvider = dbContextProvider;
            _idGenerator = idGenerator;
        }

        /// <summary>
        ///     Send a message to a specified destination (aka topic in Kafka).
        /// </summary>
        /// <param name="destination">Destination channel (topic) to publish to</param>
        /// <param name="message">Message to publish</param>
        public void Send(string destination, IMessage message)
        {
            var logContext = $"{nameof(Send)} destination='{destination}'";
            Logger.LogDebug($"+{logContext}");
            var id = _idGenerator.GenerateId().AsString();
            SendMessage(id, destination, message, this);
            Logger.LogDebug($"-{logContext}: sent message id='{id}'");
        }

        /// <summary>
        ///     Send a message to a specified destination (aka topic in Kafka).
        /// </summary>
        /// <param name="destination">Destination channel (topic) to publish to</param>
        /// <param name="message">Message to publish</param>
        public async Task SendAsync(string destination, IMessage message)
        {
            var logContext = $"{nameof(Send)} destination='{destination}'";
            Logger.LogDebug($"+{logContext}");
            var id = _idGenerator.GenerateId().AsString();
            await SendMessageAsync(id, destination, message, this);
            Logger.LogDebug($"-{logContext}: sent message id='{id}'");
        }


        /// <summary>
        ///     Send message the message in the database to be processed
        ///     by the CDC.
        /// </summary>
        /// <param name="message">Message to publish</param>
        public void Send(IMessage message)
        {
            // Relies on db column default value to set CreationTime
            var messageEntity = new Message(message);
            using var dbContext = _dbContextProvider.CreateDbContext();
            dbContext.Messages.Add(messageEntity);
            dbContext.SaveChanges();
        }

        /// <summary>
        ///     Send message the message in the database to be processed
        ///     by the CDC.
        /// </summary>
        /// <param name="message">Message to publish</param>
        public async Task SendAsync(IMessage message)
        {
            // Relies on db column default value to set CreationTime
            var messageEntity = new Message(message);
            await using var dbContext = _dbContextProvider.CreateDbContext();
            await dbContext.Messages.AddAsync(messageEntity);
            await dbContext.SaveChangesAsync();
        }
    }
}