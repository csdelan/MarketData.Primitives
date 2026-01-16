namespace MarketData.Primitives
{
    public sealed class BarMetrics : IBarKey
    {
        public string Symbol { get; init; } = default!;
        public string Timeframe { get; init; } = default!;
        public DateTime TimestampUtc { get; init; }

        public string MetricSet { get; init; }  = string.Empty;
        public int MetricVersion { get; init; }
        public DateTime AsOfUtc { get; init; }

        public Dictionary<string, decimal> Values { get; init; } = new();
    }
}
