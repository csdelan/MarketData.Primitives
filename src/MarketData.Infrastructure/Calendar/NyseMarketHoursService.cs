using MarketData.Application.Contracts;
using MarketData.Primitives;

namespace MarketData.Infrastructure.Calendar;

/// <summary>
/// Consolidated NYSE market hours service providing both calendar and hours functionality.
/// </summary>
public sealed class NyseMarketHoursService(ITimeKeeper timeKeeper) : IMarketTimingService
{
    // Calendar methods
    public Task<bool> IsTradingDayAsync(string venue, DateOnly date, CancellationToken ct = default)
    {
        EnsureVenue(venue);
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return Task.FromResult(false);

        bool isHoliday = GetDefaultHolidays(date.Year).Contains(date);
        return Task.FromResult(!isHoliday);
    }

    public async Task<MarketSession?> GetSessionAsync(string venue, DateOnly date, CancellationToken ct = default)
    {
        if (!await IsTradingDayAsync(venue, date, ct))
            return null;

        return new MarketSession(new TimeOnly(9, 30), new TimeOnly(16, 0));
    }

    public Task<IReadOnlyList<DateOnly>> GetHolidaysAsync(string venue, int year, CancellationToken ct = default)
    {
        EnsureVenue(venue);
        IReadOnlyList<DateOnly> holidays = GetDefaultHolidays(year);
        return Task.FromResult(holidays);
    }

    // Hours methods
    public async Task<bool> IsOpenAsync(string venue, DateTimeOffset asOfUtc, CancellationToken ct = default)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(asOfUtc, MarketTimeZoneProvider.EasternTimeZone);
        DateOnly date = DateOnly.FromDateTime(local.DateTime);

        if (!await IsTradingDayAsync(venue, date, ct))
            return false;

        var session = await GetSessionAsync(venue, date, ct);
        if (session is null)
            return false;

        var localTime = TimeOnly.FromDateTime(local.DateTime);
        return localTime >= session.Open && localTime < session.Close;
    }

    public async Task<MarketHoursStatus> GetCurrentStatusAsync(string venue, CancellationToken ct = default)
    {
        DateTimeOffset asOfUtc = timeKeeper.Now;
        DateTimeOffset asOfLocal = TimeZoneInfo.ConvertTime(asOfUtc, MarketTimeZoneProvider.EasternTimeZone);
        DateOnly date = DateOnly.FromDateTime(asOfLocal.DateTime);

        bool isTradingDay = await IsTradingDayAsync(venue, date, ct);
        var session = isTradingDay
            ? await GetSessionAsync(venue, date, ct)
            : null;

        var localTime = TimeOnly.FromDateTime(asOfLocal.DateTime);
        bool isOpen = session is not null && localTime >= session.Open && localTime < session.Close;

        return new MarketHoursStatus(isTradingDay, isOpen, asOfLocal, session);
    }

    private static IReadOnlyList<DateOnly> GetDefaultHolidays(int year)
    {
        return
        [
            ObserveHoliday(new DateOnly(year, 1, 1)),
            ObserveHoliday(new DateOnly(year, 7, 4)),
            ObserveHoliday(new DateOnly(year, 12, 25))
        ];
    }

    private static DateOnly ObserveHoliday(DateOnly holiday) => holiday.DayOfWeek switch
    {
        DayOfWeek.Saturday => holiday.AddDays(-1),
        DayOfWeek.Sunday => holiday.AddDays(1),
        _ => holiday
    };

    private static void EnsureVenue(string venue)
    {
        if (!string.Equals(venue, "NYSE", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Venue '{venue}' is not supported by {nameof(NyseMarketHoursService)}.");
    }
}
