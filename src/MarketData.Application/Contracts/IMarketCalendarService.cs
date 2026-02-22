namespace MarketData.Application.Contracts;

public interface IMarketCalendarService
{
    Task<bool> IsTradingDayAsync(string venue, DateOnly date, CancellationToken ct = default);

    Task<MarketSession?> GetSessionAsync(string venue, DateOnly date, CancellationToken ct = default);

    Task<IReadOnlyList<DateOnly>> GetHolidaysAsync(string venue, int year, CancellationToken ct = default);
}
