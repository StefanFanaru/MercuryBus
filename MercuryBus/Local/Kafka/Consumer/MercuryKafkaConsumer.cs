using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Local.Kafka.Consumer
{
    /// <summary>
    ///     Kafka consumer listens for a set of topics and triggers a callback when
    ///     an event is received.
    ///     Disposing of the the consumer shuts down the subscription.
    /// </summary>
    public sealed class MercuryKafkaConsumer : IDisposable
    {
        private const int ConsumePollMilliseconds = 100;
        private const int AdminClientTimeoutMilliseconds = 10;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private readonly IDictionary<string, string> _consumerProperties;
        private readonly MercuryKafkaConsumerMessageHandler _handler;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        private readonly string _subscriberId;
        private readonly IList<string> _topics;

        private volatile MercuryKafkaConsumerState _state = MercuryKafkaConsumerState.Created;

        public MercuryKafkaConsumer(string subscriberId,
            MercuryKafkaConsumerMessageHandler handler,
            IList<string> topics,
            string bootstrapServers,
            MercuryKafkaConsumerConfigurationProperties mercuryKafkaConsumerConfigurationProperties,
            ILoggerFactory loggerFactory)
        {
            _subscriberId = subscriberId;
            _handler = handler;
            _topics = topics;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<MercuryKafkaConsumer>();

            _consumerProperties =
                ConsumerPropertiesFactory.MakeDefaultConsumerProperties(bootstrapServers, subscriberId)
                    .ToDictionary(p => p.Key, p => p.Value);

            foreach (var (key, value) in mercuryKafkaConsumerConfigurationProperties.Properties)
            {
                _consumerProperties[key] = value;
            }
        }

        public void Dispose()
        {
            var logContext = $"{nameof(Dispose)} for SubscriberId={_subscriberId}";
            _logger.LogDebug($"+{logContext}");
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _logger.LogDebug($"+{logContext}: Sending cancel to consumer thread.");
                _cancellationTokenSource.Cancel();
            }

            _cancellationTokenSource.Dispose();
            _logger.LogDebug($"-{logContext}");
        }

        private void VerifyTopicExistsBeforeSubscribing(IAdminClient adminClient, string topic)
        {
            var logContext = $"{nameof(VerifyTopicExistsBeforeSubscribing)} " +
                             $"for subscriberId='{_subscriberId}', topic='{topic}'";
            try
            {
                _logger.LogDebug($"+{logContext}");
                var metadata = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(AdminClientTimeoutMilliseconds));

                var partitions = metadata.Topics[0].Partitions;
                _logger.LogDebug($"-{logContext}: found partitions='{string.Join(",", partitions.Select(p => p.PartitionId))}'");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{logContext}: Got exception: {e}");
                throw;
            }
        }

        private void MaybeCommitOffsets(IConsumer<string, string> consumer, KafkaMessageProcessor processor)
        {
            var logContext = $"{nameof(MaybeCommitOffsets)} for SubscriberId='{_subscriberId}'";
            var offsetsToCommit = processor.OffsetsToCommit().ToList();
            if (offsetsToCommit.Any())
            {
                _logger.LogDebug(
                    $"{logContext}: Committing {offsetsToCommit.Count} offsets='{string.Join(",", offsetsToCommit)}'");
                consumer.Commit(offsetsToCommit);
                processor.NoteOffsetsCommitted(offsetsToCommit);
                _logger.LogDebug($"-{logContext}");
            }
        }

        public void Start()
        {
            var logContext = $"{nameof(Start)} for SubscriberId={_subscriberId}";
            try
            {
                var consumer = new ConsumerBuilder<string, string>(_consumerProperties).Build();
                var processor = new KafkaMessageProcessor(_subscriberId, _handler,
                    _loggerFactory.CreateLogger<KafkaMessageProcessor>());

                using (var adminClient = new DependentAdminClientBuilder(consumer.Handle).Build())
                {
                    foreach (var topic in _topics)
                    {
                        VerifyTopicExistsBeforeSubscribing(adminClient, topic);
                    }
                }

                var topicsList = new List<string>(_topics);
                _logger.LogDebug($"{logContext}: Subscribing to topics='{string.Join(",", topicsList)}'");

                consumer.Subscribe(topicsList);

                // Set state to started before starting the processing thread instead of after
                // (prevent setting it to it started after it has potentially already been set to stopped)
                _state = MercuryKafkaConsumerState.Started;

                Task.Run(() => { StartConsuming(consumer, processor); }, _cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{logContext}: Error subscribing");
                _state = MercuryKafkaConsumerState.FailedToStart;
                throw;
            }
        }

        private void StartConsuming(IConsumer<string, string> consumer, KafkaMessageProcessor processor)
        {
            var logContext = $"{nameof(StartConsuming)} for SubscriberId={_subscriberId}";
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    ConsumeAndPublish(consumer, processor);
                }

                _state = MercuryKafkaConsumerState.Stopped;
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation($"{logContext}: Shutdown by cancel");
                _state = MercuryKafkaConsumerState.Stopped;
            }
            catch (KafkaMessageProcessorFailedException e)
            {
                _logger.LogError($"{logContext}: Terminating due to KafkaMessageProcessorFailedException - {e}");
                _state = MercuryKafkaConsumerState.MessageHandlingFailed;
            }
            catch (Exception e)
            {
                _logger.LogError($"{logContext}: Exception - {e}");
                _state = MercuryKafkaConsumerState.Failed;
            }
            finally
            {
                // Try to put the last of the offsets away. Note that the
                // callbacks are done asynchronously so there is no guarantee
                // that all the offsets are ready. Worst case is that there
                // are messages processed more than once.
                MaybeCommitOffsets(consumer, processor);
                consumer.Dispose();

                _logger.LogDebug($"{logContext}: Stopped in state {_state.ToString()}");
            }
        }

        private void ConsumeAndPublish(IConsumer<string, string> consumer, KafkaMessageProcessor processor)
        {
            var logContext = $"{nameof(ConsumeAndPublish)} for SubscriberId={_subscriberId}";
            try
            {
                var record = consumer.Consume(TimeSpan.FromMilliseconds(ConsumePollMilliseconds));

                if (record != null)
                {
                    _logger.LogDebug(
                        $"{logContext}: process record at offset='{record.Offset}', key='{record.Message.Key}', value='{record.Message.Value}'");

                    processor.Process(record);
                }
                else
                {
                    processor.ThrowExceptionIfHandlerFailed();
                }

                MaybeCommitOffsets(consumer, processor);
            }
            catch (ConsumeException e)
            {
                _logger.LogError($"{logContext}: ConsumeException - {e.Error}. Continuing.");
            }
        }
    }
}
