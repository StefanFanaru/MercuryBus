using System;
using System.Threading;

namespace MercuryBus.Helpers
{
    public class TimingProvider : ITimingProvider
    {
        public void DelayMilliseconds(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        public long GetNowMilliseconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}