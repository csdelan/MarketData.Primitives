using MarketData.Primitives;

namespace MarketData.Application.Contracts;

/// <summary>
/// The trading-day ordinal (1-based) of a date within each of its containing periods.
/// A value of 0 means the date is not a trading day.
/// </summary>
public sealed record TradingDayOrdinal(
    int OfWeek,
    int OfMonth,
    int OfQuarter,
    int OfYear);

/// <summary>
/// Trading-day counts for a period (week/month/quarter/year) containing a reference date.
/// </summary>
/// <param name="Period">The period unit (Weeks, Months, Quarters, or Years).</param>
/// <param name="PeriodStart">First calendar date of the period.</param>
/// <param name="PeriodEndInclusive">Last calendar date of the period.</param>
/// <param name="Total">Total trading days in the period.</param>
/// <param name="Elapsed">Trading days on or before the reference date (inclusive of the reference day when it trades).</param>
/// <param name="Remaining">Total minus Elapsed.</param>
public sealed record TradingPeriodStats(
    ResolutionUnit Period,
    DateOnly PeriodStart,
    DateOnly PeriodEndInclusive,
    int Total,
    int Elapsed,
    int Remaining);
