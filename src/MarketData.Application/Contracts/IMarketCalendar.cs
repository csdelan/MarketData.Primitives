using MarketData.Primitives;
using MarketData.Primitives.Sessions;

namespace MarketData.Application.Contracts;

/// <summary>
/// Pure, deterministic, clock-free market calendar. Every method takes explicit dates,
/// so all calendar math is trivially testable without a clock or timezone "now".
/// </summary>
public interface IMarketCalendar
{
    /// <summary>The venue this calendar represents.</summary>
    string VenueId { get; }

    /// <summary>The venue's intraday phase schedule.</summary>
    VenueSchedule Schedule { get; }

    // --- classification ---

    /// <summary>Whether the date is a trading day (regular or half day).</summary>
    bool IsTradingDay(DateOnly date);

    /// <summary>Classifies the date (regular/half/weekend/holiday) with a name when applicable.</summary>
    MarketDayInfo ClassifyDay(DateOnly date);

    /// <summary>Resolves the session windows for the date to UTC instants; null if not a trading day.</summary>
    MarketSessionWindow? GetSessionWindow(DateOnly date);

    // --- holidays ---

    /// <summary>The resolved holiday calendar (full closures and early closes) for a year.</summary>
    MarketHolidayCalendarYear GetCalendarYear(int year);

    /// <summary>Attempts to get the holiday/early-close entry for a date.</summary>
    bool TryGetHoliday(DateOnly date, out Holiday holiday);

    // --- navigation ---

    /// <summary>The first trading day strictly after the date.</summary>
    DateOnly NextTradingDay(DateOnly date);

    /// <summary>The first trading day strictly before the date.</summary>
    DateOnly PreviousTradingDay(DateOnly date);

    /// <summary>Adds a signed number of trading days to a start date.</summary>
    DateOnly AddTradingDays(DateOnly start, int count);

    // --- counting / numbering ---

    /// <summary>Counts trading days in [start, end] inclusive. Returns a negative count if start &gt; end.</summary>
    int CountTradingDays(DateOnly startInclusive, DateOnly endInclusive);

    /// <summary>The ISO-8601 week number of the date.</summary>
    int IsoWeekNumber(DateOnly date);

    /// <summary>The trading-day ordinals of the date within its week/month/quarter/year (0 if not a trading day).</summary>
    TradingDayOrdinal GetTradingDayOrdinal(DateOnly date);

    /// <summary>Trading-day statistics for the period (Weeks/Months/Quarters/Years) containing the reference date.</summary>
    TradingPeriodStats GetPeriodStats(DateOnly reference, ResolutionUnit period);

    // --- options expiration / witching ---

    /// <summary>The monthly options expiration for a year and month.</summary>
    OptionsExpiration GetMonthlyExpiration(int year, int month);

    /// <summary>The quarterly options expiration for a year and quarter (1..4).</summary>
    OptionsExpiration GetQuarterlyExpiration(int year, int quarter);

    /// <summary>The four quarterly witching expirations for a year.</summary>
    IReadOnlyList<OptionsExpiration> GetWitchingDates(int year);

    /// <summary>The next expiration on or after the date; quarterly-only when requested.</summary>
    OptionsExpiration NextExpirationOnOrAfter(DateOnly date, bool quarterlyOnly = false);

    /// <summary>The settlement date for a trade, advancing by a number of trading days (default T+1).</summary>
    DateOnly SettlementDate(DateOnly tradeDate, int tradingDays = 1);
}
