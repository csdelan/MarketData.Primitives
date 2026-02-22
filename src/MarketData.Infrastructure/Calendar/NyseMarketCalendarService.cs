using MarketData.Application.Contracts;

namespace MarketData.Infrastructure.Calendar;

public sealed class NyseMarketCalendarService : IMarketCalendarService
{
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
            throw new NotSupportedException($"Venue '{venue}' is not supported by {nameof(NyseMarketCalendarService)}.");
    }
}
