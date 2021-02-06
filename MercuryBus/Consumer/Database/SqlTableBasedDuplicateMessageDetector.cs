using System;
using System.Transactions;
using MercuryBus.Consumer.Common;
using MercuryBus.Database;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Consumer.Database
{
    public class SqlTableBasedDuplicateMessageDetector : IDuplicateMessageDetector
    {
        private readonly IMercuryBusDbContextProvider _dbContextProvider;
        private readonly ILogger<SqlTableBasedDuplicateMessageDetector> _logger;

        public SqlTableBasedDuplicateMessageDetector(IMercuryBusDbContextProvider dbContextProvider,
            ILogger<SqlTableBasedDuplicateMessageDetector> logger)
        {
            _dbContextProvider = dbContextProvider;
            _logger = logger;
        }

        public bool IsDuplicate(string consumerId, string messageId)
        {
            var logContext = $"{nameof(IsDuplicate)} " +
                             $"for consumerId='{consumerId}', messageId='{messageId}'";
            try
            {
                _logger.LogDebug($"+{logContext}");
                using (var context = _dbContextProvider.CreateDbContext())
                {
                    context.ReceivedMessages.Add(new ReceivedMessage
                    {
                        ConsumerId = consumerId,
                        MessageId = messageId
                    });
                    context.SaveChanges();
                }

                _logger.LogDebug($"-{logContext}");
                return false;
            }
            catch (DbUpdateException e)
            {
                const int duplicateKeyError = 2627;
                if (e.InnerException is SqlException {Number: duplicateKeyError})
                {
                    _logger.LogDebug($"{logContext}: Detected duplicate");
                    return true;
                }

                _logger.LogError(e, $"{logContext}: Got an exception");
                throw;
            }
        }

        public void DoWithMessage(SubscriberIdAndMessage subscriberIdAndMessage, Action callback)
        {
            var logContext = $"{nameof(IsDuplicate)} " +
                             $"for subscriberId='{subscriberIdAndMessage.SubscriberId}', messageId='{subscriberIdAndMessage.SubscriberId}'";

            using var transactionScope = new TransactionScope();
            try
            {
                if (!IsDuplicate(subscriberIdAndMessage.SubscriberId, subscriberIdAndMessage.Message.Id))
                {
                    _logger.LogDebug($"{logContext}: Invoking handlers");
                    callback();
                }

                transactionScope.Complete();
                _logger.LogDebug($"{logContext}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{logContext}: Exception while processing message");
                throw;
            }
        }
    }
}