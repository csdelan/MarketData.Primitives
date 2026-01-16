namespace MarketData.Primitives
{
    public class Quote
    {
        public DateTime Timestamp { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal Last { get; set; }

        public decimal Spread { get { return Math.Abs(Bid - Ask); } }
        public decimal SpreadPercent { get { return Spread / Ask; } }
    }
}
