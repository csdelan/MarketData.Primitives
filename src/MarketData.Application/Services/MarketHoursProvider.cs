using MarketData.Application.Contracts;

namespace MarketData.Application.Services;

/// <summary>
/// Facade provider for market hours and timing operations.
/// Coordinates calls to the unified market timing service.
/// </summary>
public class MarketHoursProvider
{
    private readonly IMarketTimingService _timingService;

    public MarketHoursProvider(IMarketTimingService timingService)
    {
        _timingService = timingService;
    }

    /// <summary>
    /// Determines if the market is currently open at the specified time.
    /// </summary>
    public bool IsMarketOpen(DateTime dateTime)
    {
        var offset = new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
        return _timingService.IsOpenAsync("NYSE", offset).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the next market open time after the specified date/time.
    /// </summary>
    public DateTime GetNextMarketOpen(DateTime dateTime)
    {
        DateTime nextDay = dateTime.Date.AddDays(1);
        while (true)
        {
            if (_timingService.IsTradingDayAsync("NYSE", DateOnly.FromDateTime(nextDay)).GetAwaiter().GetResult())
            {
                var session = _timingService.GetSessionAsync("NYSE", DateOnly.FromDateTime(nextDay)).GetAwaiter().GetResult();
                if (session != null)
                {
                    return new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, session.Open.Hour, session.Open.Minute, 0);
                }
            }
            nextDay = nextDay.AddDays(1);
        }
    }
}
