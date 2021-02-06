namespace MercuryBus.Helpers
{
    public interface ITimingProvider
    {
        void DelayMilliseconds(int milliseconds);
        long GetNowMilliseconds();
    }
}