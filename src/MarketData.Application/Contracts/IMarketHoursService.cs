namespace MarketData.Application.Contracts;

public interface IMarketHoursService
{
    Task<bool> IsOpenAsync(string venue, DateTimeOffset asOfUtc, CancellationToken ct = default);

    Task<MarketHoursStatus> GetCurrentStatusAsync(string venue, CancellationToken ct = default);
}
