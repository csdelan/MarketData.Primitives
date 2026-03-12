using System.Text.Json.Serialization;

namespace MarketData.Primitives
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SecurityType
    {
        Unknown = 0,
        Stock = 1,
        Future = 2,
        ETF = 3,
        Index = 4,
        StockOption = 5,
        FutureOption = 6,
        Crypto = 7,
        Currency = 8,
        Bond = 9,
        MutualFund = 10
    }
}
