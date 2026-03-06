using System.Text.Json;
using MarketData.Application.Contracts;
using MarketData.Primitives;

namespace MarketData.Infrastructure.Calendar;

/// <summary>
/// Consolidated NYSE market hours service providing both calendar and hours functionality.
/// Supports configurable holidays and half-days loaded from JSON configuration files.
/// </summary>
public sealed class NyseMarketHoursService(ITimeKeeper timeKeeper, string? configPath = null) : IMarketTimingService
{
    private readonly string _configPath = configPath ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "OneDrive", "TradingSystem", "config");

    private readonly Dictionary<int, HolidayConfig> _holidayCache = [];

    /// <summary>
    /// Determines whether a given date is a trading day (not a weekend, holiday, or half-day closure).
    /// </summary>
    /// <param name="venue">The trading venue (NYSE only).</param>
    /// <param name="date">The date to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the date is a trading day, false otherwise.</returns>
    public Task<bool> IsTradingDayAsync(string venue, DateOnly date, CancellationToken ct = default)
    {
        EnsureVenue(venue);
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return Task.FromResult(false);

        var holidays = GetHolidays(date.Year);
        bool isHoliday = holidays.Holidays.Contains(date);
        return Task.FromResult(!isHoliday);
    }

    /// <summary>
    /// Gets the market session hours for a given date.
    /// </summary>
    /// <param name="venue">The trading venue (NYSE only).</param>
    /// <param name="date">The date to get session for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="MarketSession"/> with open/close times, or null if not a trading day.</returns>
    public async Task<MarketSession?> GetSessionAsync(string venue, DateOnly date, CancellationToken ct = default)
    {
        if (!await IsTradingDayAsync(venue, date, ct))
            return null;

        var holidays = GetHolidays(date.Year);
        bool isHalfDay = holidays.HalfDays.Contains(date);

        TimeOnly closeTime = isHalfDay ? new TimeOnly(13, 0) : new TimeOnly(16, 0);
        return new MarketSession(new TimeOnly(9, 30), closeTime);
    }

    /// <summary>
    /// Gets the list of holiday dates for a given year.
    /// </summary>
    /// <param name="venue">The trading venue (NYSE only).</param>
    /// <param name="year">The year for which to retrieve holidays.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of holiday dates.</returns>
    public Task<IReadOnlyList<DateOnly>> GetHolidaysAsync(string venue, int year, CancellationToken ct = default)
    {
        EnsureVenue(venue);
        var holidays = GetHolidays(year);
        return Task.FromResult((IReadOnlyList<DateOnly>)holidays.Holidays);
    }

    /// <summary>
    /// Determines whether the market is currently open.
    /// </summary>
    /// <param name="venue">The trading venue (NYSE only).</param>
    /// <param name="asOfUtc">The point in time to check (in UTC).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the market is open at the specified time, false otherwise.</returns>
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

    /// <summary>
    /// Gets today's regular-session close time expressed as UTC.
    /// </summary>
    /// <param name="venue">The trading venue (NYSE only).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The close time in UTC, or null if today is not a trading day.</returns>
    public async Task<DateTimeOffset?> GetTodayCloseUtcAsync(string venue, CancellationToken ct = default)
    {
        EnsureVenue(venue);

        // Resolve "today" in Eastern time, since that's where NYSE lives.
        DateTimeOffset asOfEastern = TimeZoneInfo.ConvertTime(timeKeeper.Now, MarketTimeZoneProvider.EasternTimeZone);
        DateOnly today = DateOnly.FromDateTime(asOfEastern.DateTime);

        var session = await GetSessionAsync(venue, today, ct);
        if (session is null) return null;   // weekend or holiday → no close today

        // Build a DateTimeOffset for today's close in Eastern time, then express as UTC.
        var closeDateTime = today.ToDateTime(session.Close);
        var easternOffset = MarketTimeZoneProvider.EasternTimeZone.GetUtcOffset(closeDateTime);
        return new DateTimeOffset(closeDateTime, easternOffset).ToUniversalTime();
    }

    /// <summary>
    /// Gets the current market status (trading day, open status, session hours).
    /// </summary>
    /// <param name="venue">The trading venue (NYSE only).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="MarketHoursStatus"/> with current market information.</returns>
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

    private HolidayConfig GetHolidays(int year)
    {
        if (_holidayCache.TryGetValue(year, out var cached))
            return cached;

        var config = LoadHolidayConfig(year) ?? GetDefaultHolidays(year);
        _holidayCache[year] = config;
        return config;
    }

    private HolidayConfig? LoadHolidayConfig(int year)
    {
        try
        {
            string configFile = Path.Combine(_configPath, "holidays", $"holidays-{year}.json");
            if (!File.Exists(configFile))
                return null;

            string json = File.ReadAllText(configFile);
            return JsonSerializer.Deserialize<HolidayConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static HolidayConfig GetDefaultHolidays(int year)
    {
        var holidays = new List<DateOnly>
        {
            ObserveHoliday(new DateOnly(year, 1, 1)),      // New Year's Day
            GetMLKDay(year),                                // MLK Jr. Day
            GetPresidentsDay(year),                         // Presidents' Day
            GetGoodFriday(year),                            // Good Friday
            GetMemorialDay(year),                           // Memorial Day
            ObserveHoliday(new DateOnly(year, 7, 4)),      // Independence Day
            GetLaborDay(year),                              // Labor Day
            GetThanksgivingDay(year),                       // Thanksgiving
            ObserveHoliday(new DateOnly(year, 12, 25))     // Christmas
        };

        // Half-days: Day after Thanksgiving, Christmas Eve
        var halfDays = new List<DateOnly>
        {
            GetThanksgivingDay(year).AddDays(1),           // Day after Thanksgiving
            new DateOnly(year, 12, 24)                      // Christmas Eve
        };

        return new HolidayConfig { Holidays = holidays, HalfDays = halfDays };
    }

    private static DateOnly ObserveHoliday(DateOnly holiday) => holiday.DayOfWeek switch
    {
        DayOfWeek.Saturday => holiday.AddDays(-1),
        DayOfWeek.Sunday => holiday.AddDays(1),
        _ => holiday
    };

    private static DateOnly GetMLKDay(int year)
    {
        // Third Monday of January
        var jan = new DateOnly(year, 1, 1);
        int daysUntilMonday = (int)DayOfWeek.Monday - (int)jan.DayOfWeek;
        if (daysUntilMonday < 0) daysUntilMonday += 7;
        return jan.AddDays(daysUntilMonday + 14);
    }

    private static DateOnly GetPresidentsDay(int year)
    {
        // Third Monday of February
        var feb = new DateOnly(year, 2, 1);
        int daysUntilMonday = (int)DayOfWeek.Monday - (int)feb.DayOfWeek;
        if (daysUntilMonday < 0) daysUntilMonday += 7;
        return feb.AddDays(daysUntilMonday + 14);
    }

    private static DateOnly GetGoodFriday(int year)
    {
        // Calculate Easter Sunday using Computus algorithm, then subtract 2 days for Good Friday
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;
        var easter = new DateOnly(year, month, day);
        return easter.AddDays(-2);
    }

    private static DateOnly GetMemorialDay(int year)
    {
        // Last Monday of May
        var lastDay = new DateOnly(year, 5, 31);
        while (lastDay.DayOfWeek != DayOfWeek.Monday)
            lastDay = lastDay.AddDays(-1);
        return lastDay;
    }

    private static DateOnly GetLaborDay(int year)
    {
        // First Monday of September
        var sept = new DateOnly(year, 9, 1);
        int daysUntilMonday = (int)DayOfWeek.Monday - (int)sept.DayOfWeek;
        if (daysUntilMonday < 0) daysUntilMonday += 7;
        return sept.AddDays(daysUntilMonday);
    }

    private static DateOnly GetThanksgivingDay(int year)
    {
        // Fourth Thursday of November
        var nov = new DateOnly(year, 11, 1);
        int daysUntilThursday = (int)DayOfWeek.Thursday - (int)nov.DayOfWeek;
        if (daysUntilThursday < 0) daysUntilThursday += 7;
        return nov.AddDays(daysUntilThursday + 21);
    }

    private static void EnsureVenue(string venue)
    {
        if (!string.Equals(venue, "NYSE", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Venue '{venue}' is not supported by {nameof(NyseMarketHoursService)}.");
    }
}

/// <summary>
/// Configuration for NYSE holidays and half-days for a given year.
/// </summary>
public class HolidayConfig
{
    /// <summary>
    /// List of dates when the market is completely closed.
    /// </summary>
    public List<DateOnly> Holidays { get; set; } = [];

    /// <summary>
    /// List of dates when the market closes at 1:00 PM (13:00).
    /// </summary>
    public List<DateOnly> HalfDays { get; set; } = [];
}
