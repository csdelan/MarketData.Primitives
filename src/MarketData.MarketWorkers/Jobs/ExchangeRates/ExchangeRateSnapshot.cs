using MarketData.Workers;

namespace MarketData.ServiceWorkers.Jobs;

/// <summary>
/// A single FiscalData "rate of exchange" observation, stored as a time-series document keyed by
/// record date + currency.
/// </summary>
public sealed record ExchangeRateSnapshot : TimeSeriesDocument
{
    /// <summary>The <c>country_currency_desc</c> value, e.g. "Euro Zone-Euro".</summary>
    public required string CountryCurrency { get; init; }

    /// <summary>The <c>exchange_rate</c> value.</summary>
    public required decimal ExchangeRate { get; init; }

    /// <summary>The <c>record_date</c> the rate was published for.</summary>
    public required DateOnly RecordDate { get; init; }

    public override string Id => $"{RecordDate:yyyy-MM-dd}_{Slug(CountryCurrency)}";
}
