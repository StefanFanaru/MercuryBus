using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Local.Kafka.Consumer
{
    /// <summary>
    ///     Keeps track of message offsets that are (a) being processed and (b) have been processed and can be committed
    /// </summary>
    public class OffsetTracker
    {
        private readonly ILogger _logger;

        private readonly IDictionary<TopicPartition, TopicPartitionOffsets> _state =
            new Dictionary<TopicPartition, TopicPartitionOffsets>();

        public OffsetTracker(ILogger logger)
        {
            _logger = logger;
        }

        public override string ToString()
        {
            // Output the collection of topics and offsets
            var stringBuilder = new StringBuilder($"{nameof(_state)}: ");
            foreach (var (key, value) in _state)
            {
                stringBuilder.Append($"{key}={value},");
            }

            if (_state.Count > 0)
            {
                // Remove the last comma
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        private TopicPartitionOffsets Fetch(TopicPartition topicPartition)
        {
            var logContext = $"OffsetTracker.Fetch for topicPartition='{topicPartition}'";
            _logger.LogDebug($"+{logContext}");
            _state.TryGetValue(topicPartition, out var tpo);
            if (tpo == null)
            {
                _logger.LogDebug($"{logContext}: Creating new topic partition offset");
                tpo = new TopicPartitionOffsets(_logger);
                _state[topicPartition] = tpo;
            }

            _logger.LogDebug($"-{logContext}");
            return tpo;
        }

        public void NoteUnprocessed(TopicPartition topicPartition, long offset)
        {
            var logContext = $"OffsetTracker.NoteUnprocessed for topicPartition='{topicPartition}', offset={offset}";
            _logger.LogDebug($"{logContext}");
            Fetch(topicPartition).NoteUnprocessed(offset);
        }

        public void NoteProcessed(TopicPartition topicPartition, long offset)
        {
            var logContext = $"OffsetTracker.NoteProcessed for topicPartition='{topicPartition}', offset={offset}";
            _logger.LogDebug($"{logContext}");
            Fetch(topicPartition).NoteProcessed(offset);
        }

        public IEnumerable<TopicPartitionOffset> OffsetsToCommit()
        {
            var logContext = "OffsetTracker.OffsetsToCommit";
            _logger.LogTrace($"+{logContext}");
            var result = new List<TopicPartitionOffset>();
            foreach (var (key, value) in _state)
            {
                var offset = value.OffsetToCommit();
                if (offset.HasValue)
                {
                    result.Add(new TopicPartitionOffset(key, new Offset(offset.Value + 1)));
                }
            }

            _logger.LogTrace($"-{logContext}: collected {result.Count} offsets to commit");
            return result;
        }

        public void NoteOffsetsCommitted(IEnumerable<TopicPartitionOffset> offsetsToCommit)
        {
            var offsets = new List<TopicPartitionOffset>(offsetsToCommit);
            var logContext = $"OffsetTracker.NoteOffsetsCommitted with {offsets.Count} offsets";
            _logger.LogDebug($"+{logContext}");
            foreach (var topicPartitionOffset in offsets)
            {
                Fetch(topicPartitionOffset.TopicPartition).NoteOffsetCommitted(topicPartitionOffset.Offset);
            }

            _logger.LogDebug($"-{logContext}");
        }
    }
}