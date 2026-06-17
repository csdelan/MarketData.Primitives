using System.Net.Http.Json;
using Core.Json;
using Microsoft.Extensions.Options;

namespace MarketData.ServiceWorkers.Jobs;

/// <summary>
/// Typed client for the U.S. Treasury FiscalData "rates of exchange" dataset. Registered via
/// <c>IHttpClientFactory</c> with a standard Polly resilience handler (retry/timeout/circuit-breaker),
/// so this class holds no resilience logic of its own.
/// </summary>
public sealed class TreasuryFiscalDataClient
{
    private const string Path = "v1/accounting/od/rates_of_exchange";

    private readonly HttpClient _http;
    private readonly TreasuryApiOptions _options;

    public TreasuryFiscalDataClient(HttpClient http, IOptions<TreasuryApiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    /// <summary>Fetches exchange-rate rows with <c>record_date</c> on or after the configured <c>FromDate</c>.</summary>
    public async Task<RatesOfExchangeResponse> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{Path}?fields=country_currency_desc,exchange_rate,record_date" +
                  $"&filter=record_date:gte:{_options.FromDate}";

        var response = await _http.GetFromJsonAsync<RatesOfExchangeResponse>(
            url, CoreJson.Default, cancellationToken);

        return response ?? new RatesOfExchangeResponse();
    }
}
