using System;
using System.Collections.Concurrent;
using MercuryBus.Messaging.Common;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Consumer.Kafka
{
    public class SwimlaneBasedDispatcher
    {
        private readonly string _dispatcherContext;
        private readonly ILogger<SwimlaneBasedDispatcher> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConcurrentDictionary<int, SwimlaneDispatcher> _map = new();
        private readonly string _subscriberId;

        public SwimlaneBasedDispatcher(string subscriberId, ILoggerFactory loggerFactory)
        {
            _subscriberId = subscriberId;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<SwimlaneBasedDispatcher>();
            _dispatcherContext = $"subscriberId='{subscriberId}'";
        }

        public void Dispatch(IMessage message, int swimlane, Action<IMessage> target)
        {
            var logContext = $"{nameof(Dispatch)} for {_dispatcherContext}, swimlane='{swimlane}'";
            _logger.LogDebug($"+{logContext}");
            if (!_map.TryGetValue(swimlane, out var swimlaneDispatcher))
            {
                _logger.LogDebug($"{logContext}: No dispatcher found, attempting to create...");
                swimlaneDispatcher =
                    new SwimlaneDispatcher(_subscriberId, swimlane, _loggerFactory.CreateLogger<SwimlaneDispatcher>());
                var dispatcher = _map.GetOrAdd(swimlane, swimlaneDispatcher);
                if (dispatcher != swimlaneDispatcher)
                {
                    _logger.LogDebug($"{logContext}: Using concurrently created SwimlaneDispatcher");
                    swimlaneDispatcher = dispatcher;
                }
                else
                {
                    _logger.LogDebug($"{logContext}: Using newly created SwimlaneDispatcher");
                }
            }

            swimlaneDispatcher.Dispatch(message, target);
            _logger.LogDebug($"-{logContext}");
        }
    }
}