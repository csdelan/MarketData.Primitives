using System.Text.Json.Serialization;

namespace MarketData.ServiceWorkers.Jobs;

/// <summary>Envelope returned by the FiscalData <c>rates_of_exchange</c> endpoint.</summary>
public sealed record RatesOfExchangeResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<ExchangeRateRecord> Data { get; init; } = [];
}

/// <summary>One row of the FiscalData response (only the requested fields).</summary>
public sealed record ExchangeRateRecord
{
    [JsonPropertyName("country_currency_desc")]
    public string CountryCurrencyDesc { get; init; } = string.Empty;

    [JsonPropertyName("exchange_rate")]
    public string ExchangeRate { get; init; } = string.Empty;

    [JsonPropertyName("record_date")]
    public string RecordDate { get; init; } = string.Empty;
}
