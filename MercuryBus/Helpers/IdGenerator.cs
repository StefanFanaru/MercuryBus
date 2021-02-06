using System;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Helpers
{
    public class IdGenerator : IIdGenerator
    {
        private const long MaxCounter = 1 << 16;
        private readonly object _lockObject = new();
        private readonly ILogger<IdGenerator> _logger;
        private readonly long _macAddress;
        private readonly ITimingProvider _timingProvider;
        private long _counter;
        private long _currentPeriod;

        public IdGenerator(ITimingProvider timingProvider, ILogger<IdGenerator> logger)
        {
            _timingProvider = timingProvider;
            _logger = logger;

            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            long macAddress = interfaces.Select(x =>
            {
                var address = x.GetPhysicalAddress();
                var macAddressBytes = address.GetAddressBytes();

                if (macAddressBytes.Length != 6)
                {
                    _logger.LogTrace($"Skipping MAC address {address}");
                    return 0L;
                }

                return ToLong(macAddressBytes);
            }).FirstOrDefault(x => x != 0L);

            if (macAddress == default)
            {
                throw new InvalidOperationException("Cannot find MAC address");
            }

            _macAddress = macAddress;
            _currentPeriod = _timingProvider.GetNowMilliseconds();
            _logger.LogDebug($"MAC address {_macAddress}");
        }

        public Int128 GenerateId()
        {
            lock (_lockObject)
            {
                return GenerateIdInternal();
            }
        }

        private Int128 GenerateIdInternal()
        {
            long now = _timingProvider.GetNowMilliseconds();
            if (_currentPeriod != now || _counter == MaxCounter)
            {
                long oldPeriod = _currentPeriod;
                while ((_currentPeriod = _timingProvider.GetNowMilliseconds()) <= oldPeriod)
                {
                    _logger.LogDebug("Need to delay 1ms to reset the counter");
                    _timingProvider.DelayMilliseconds(1);
                }

                _counter = 0;
            }

            Int128 id = new Int128(_currentPeriod, (_macAddress << 16) + _counter);
            _logger.LogDebug($"Returning Id={id}, _counter={_counter}");
            _counter++;
            return id;
        }

        private static long ToLong(byte[] bytes)
        {
            long result = 0L;
            foreach (var b in bytes)
            {
                result = (result << 8) + b;
            }

            return result;
        }
    }
}
