namespace MarketData.Primitives
{
    public class RealTimeKeeper : ITimeKeeper
    {
        public RealTimeKeeper() { }
        public DateTimeOffset Now => DateTimeOffset.Now;
        public Task SetTime(DateTimeOffset time) => WaitTime(time);
        public Task WaitTime(DateTimeOffset time)
        {
            if (time <= Now) return Task.CompletedTask;
            return Task.Delay(time - Now);
        }
    }
}
