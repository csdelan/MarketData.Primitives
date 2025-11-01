namespace MarketData.Primitives
{
    public interface ITimeKeeper
    {
        DateTimeOffset Now { get; }
        Task SetTime(DateTimeOffset time);
        Task WaitTime(DateTimeOffset time);
    }
}
