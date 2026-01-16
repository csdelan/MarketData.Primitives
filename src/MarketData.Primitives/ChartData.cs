namespace MarketData.Primitives.Models
{
    public class ChartData
    {
        public required CandleSeries Candles { get; set; }
//        public List<IndicatorSeries>? IndicatorData { get; set; }
    }
}
