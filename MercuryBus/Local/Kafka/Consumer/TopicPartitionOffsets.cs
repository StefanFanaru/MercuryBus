using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Local.Kafka.Consumer
{
    /// <summary>
    ///     Tracks the offsets for a TopicPartition that are being processed or have been processed
    /// </summary>
    public class TopicPartitionOffsets
    {
        private readonly ILogger _logger;

        /// <summary>
        ///     offsets that have been processed
        /// </summary>
        private ISet<long> _processed = new HashSet<long>();

        /// <summary>
        ///     offsets that are being processed
        /// </summary>
        private SortedSet<long> _unprocessed = new();

        public TopicPartitionOffsets(ILogger logger)
        {
            _logger = logger;
        }

        public override string ToString()
        {
            return $"Unprocessed: {_unprocessed}, Processed: {_processed}";
        }

        /// <summary>
        ///     Mark the offset for an event as being processed
        /// </summary>
        /// <param name="offset">Offset of an event being processed</param>
        public void NoteUnprocessed(long offset)
        {
            _logger.LogDebug($"TopicPartitionOffset.NoteUnprocessed for offset={offset}");
            _unprocessed.Add(offset);
        }

        /// <summary>
        ///     Mark the offset of an event as processed (completed processing)
        /// </summary>
        /// <param name="offset">Offset of a processed event</param>
        public void NoteProcessed(long offset)
        {
            _logger.LogDebug($"TopicPartitionOffset.NoteProcessed for offset={offset}");
            _processed.Add(offset);
        }

        /// <summary>
        ///     Returns the highest offset that has been process. It is assumed that events are
        ///     process in order so that all lower offsets are already processed.
        /// </summary>
        /// <returns>Largest of all offsets that have been processed and can be committed</returns>
        public long? OffsetToCommit()
        {
            var logContext = "TopicPartitionOffset.OffsetToCommit";
            _logger.LogTrace($"+{logContext}");
            long? result = null;
            foreach (var x in _unprocessed)
            {
                if (_processed.Contains(x))
                {
                    result = x;
                }
                else
                {
                    break;
                }
            }

            _logger.LogTrace($"-{logContext}: returning offset={result}");
            return result;
        }

        /// <summary>
        ///     Mark an offset as committed so that all lower offsets can be
        ///     removed from processing tracking.
        ///     <remarks>
        ///         There is some off by 1 oddness in the usage so that
        ///         the offset to commit is actually the lowest not committed offset
        ///     </remarks>
        /// </summary>
        /// <param name="offset">
        ///     Offset to mark as committed implying that
        ///     all lower offsets are also committed
        /// </param>
        public void NoteOffsetCommitted(long offset)
        {
            var logContext = "TopicPartitionOffset.NoteOffsetCommitted";
            _logger.LogDebug($"+{logContext}");
            _unprocessed = new SortedSet<long>(_unprocessed.Where(x => x >= offset));
            _processed = new HashSet<long>(_processed.Where(x => x >= offset));
            _logger.LogDebug($"-{logContext}: unprocessed count={_unprocessed.Count}, " +
                             $"process count={_processed.Count} ");
        }
    }
}