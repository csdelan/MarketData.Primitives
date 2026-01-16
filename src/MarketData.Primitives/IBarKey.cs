namespace MarketData.Primitives
{
    public interface IBarKey
    {
        string Symbol { get; }
        string Timeframe { get; }
        DateTime TimestampUtc { get; }
    }
}
