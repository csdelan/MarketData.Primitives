namespace MarketData.Application.Contracts;

/// <summary>
/// Unified service for market timing, hours, and calendar information.
/// Consolidates functionality from both hours and calendar services.
/// </summary>
public interface IMarketTimingService
{
    // Calendar methods
    Task<bool> IsTradingDayAsync(string venue, DateOnly date, CancellationToken ct = default);

    Task<MarketSession?> GetSessionAsync(string venue, DateOnly date, CancellationToken ct = default);

    Task<IReadOnlyList<DateOnly>> GetHolidaysAsync(string venue, int year, CancellationToken ct = default);

    // Hours methods
    Task<bool> IsOpenAsync(string venue, DateTimeOffset asOfUtc, CancellationToken ct = default);

    Task<MarketHoursStatus> GetCurrentStatusAsync(string venue, CancellationToken ct = default);
}
