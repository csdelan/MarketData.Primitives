using System.Globalization;
using Core;
using Microsoft.Extensions.Logging;

namespace MarketData.ServiceWorkers.Jobs;

/// <summary>
/// Sample worker job: queries the public Treasury FiscalData exchange-rate API and stores each row
/// as a time-series <see cref="ExchangeRateSnapshot"/> document (JSON file DB or MongoDB, per config).
/// Demonstrates the IHttpClientFactory + IDocumentStore seams and a market-event schedule.
/// </summary>
public sealed class ExchangeRatesJob : IBackgroundJob
{
    public const string JobKey = "exchange-rates";

    private readonly TreasuryFiscalDataClient _client;
    private readonly IDocumentStore<ExchangeRateSnapshot> _store;
    private readonly TimeProvider _time;
    private readonly ILogger<ExchangeRatesJob> _logger;

    public ExchangeRatesJob(
        TreasuryFiscalDataClient client,
        IDocumentStore<ExchangeRateSnapshot> store,
        TimeProvider time,
        ILogger<ExchangeRatesJob> logger)
    {
        _client = client;
        _store = store;
        _time = time;
        _logger = logger;
    }

    public string Key => JobKey;

    public async Task ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var response = await _client.GetRatesAsync(cancellationToken);
        var now = _time.GetUtcNow();

        var saved = 0;
        foreach (var row in response.Data)
        {
            if (!TryMap(row, now, out var snapshot))
                continue;

            await _store.SaveAsync(snapshot, cancellationToken);
            saved++;
        }

        _logger.LogInformation(
            "Stored {SavedCount} of {FetchedCount} exchange-rate rows (job {JobId}).",
            saved, response.Data.Count, context.JobId);
    }

    private static bool TryMap(ExchangeRateRecord row, DateTimeOffset observedAt, out ExchangeRateSnapshot snapshot)
    {
        snapshot = null!;

        if (!decimal.TryParse(row.ExchangeRate, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
            return false;

        if (!DateOnly.TryParse(row.RecordDate, CultureInfo.InvariantCulture, out var recordDate))
            return false;

        if (string.IsNullOrWhiteSpace(row.CountryCurrencyDesc))
            return false;

        snapshot = new ExchangeRateSnapshot
        {
            CountryCurrency = row.CountryCurrencyDesc,
            ExchangeRate = rate,
            RecordDate = recordDate,
            Timestamp = observedAt,
        };
        return true;
    }
}
