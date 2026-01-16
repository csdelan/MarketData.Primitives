namespace MarketData.Primitives
{
    public interface IBar
    {
        decimal Open { get; set; }
        decimal High { get; set; }
        decimal Low { get; set; }
        decimal Close { get; set; }
        ulong Volume { get; set; }
    }

    public interface IMetric
    {
        string Name { get; set; }
        public decimal Value { get; set; }
    }
}
