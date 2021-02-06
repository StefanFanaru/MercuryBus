using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MercuryBus.Messaging.Common;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Consumer.Kafka
{
    public class SwimlaneDispatcher
    {
        private readonly string _dispatcherContext;
        private readonly object _lockObject = new();
        private readonly ILogger<SwimlaneDispatcher> _logger;
        private readonly ConcurrentQueue<QueuedMessage> _queue = new();

        // This flag must be accessed only withing a lock(_lockObject)
        private bool _running;

        public SwimlaneDispatcher(string subscriberId, int swimlane, ILogger<SwimlaneDispatcher> logger)
        {
            _logger = logger;
            _dispatcherContext = $"subscriberId='{subscriberId}', SwimLane='{swimlane}'";
        }

        public void Dispatch(IMessage message, Action<IMessage> messageConsumer)
        {
            var logContext = $"{nameof(Dispatch)} for {_dispatcherContext}, messageId={message.Id}";
            _logger.LogDebug($"+{logContext}");
            lock (_lockObject)
            {
                var queuedMessage = new QueuedMessage(message, messageConsumer);
                _queue.Enqueue(queuedMessage);
                if (!_running)
                {
                    _logger.LogDebug($"{logContext}: Added message and starting message processor");
                    _running = true;
                    StartMessageProcessor();
                }
                else
                {
                    _logger.LogDebug($"{logContext}: Added message for already running message processor");
                }

                _logger.LogDebug($"-{logContext}");
            }
        }

        private void StartMessageProcessor()
        {
            Task.Run(ProcessQueuedMessage);
        }

        private void ProcessQueuedMessage()
        {
            var logContext = $"{nameof(ProcessQueuedMessage)} for {_dispatcherContext}";
            _logger.LogDebug($"+{logContext}");
            while (true)
            {
                if (!_queue.TryDequeue(out var queuedMessage))
                {
                    // Queue was empty, check one more time with the lock on to
                    // avoid a race condition and stop the processor if really empty
                    lock (_lockObject)
                    {
                        if (!_queue.TryDequeue(out queuedMessage))
                        {
                            _logger.LogDebug($"{logContext}: No more messages, stopping message processor");
                            _running = false;
                            return;
                        }
                    }
                }

                _logger.LogDebug($"{logContext}: Invoking handler for message with ID '{queuedMessage.Message.Id}'");
                try
                {
                    queuedMessage.MessageConsumer(queuedMessage.Message);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        $"{logContext}: Exception handling message with ID '{queuedMessage.Message.Id}' - terminating...");
                    throw;
                }
            }
        }


        public class QueuedMessage
        {
            public QueuedMessage(IMessage message, Action<IMessage> messageConsumer)
            {
                Message = message;
                MessageConsumer = messageConsumer;
            }

            public IMessage Message { get; }
            public Action<IMessage> MessageConsumer { get; }
        }
    }
}