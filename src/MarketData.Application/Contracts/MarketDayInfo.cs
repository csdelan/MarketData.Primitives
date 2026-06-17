using MarketData.Primitives.Sessions;

namespace MarketData.Application.Contracts;

/// <summary>
/// Classification of a single date, with a name when it is a holiday or early-close day.
/// </summary>
/// <param name="Date">The classified date.</param>
/// <param name="Kind">Regular day, half day, weekend, or holiday.</param>
/// <param name="HolidayName">Non-null for <see cref="MarketDayKind.Holiday"/> and <see cref="MarketDayKind.HalfDay"/>.</param>
/// <param name="IsTradingDay">True for regular and half days.</param>
public sealed record MarketDayInfo(
    DateOnly Date,
    MarketDayKind Kind,
    string? HolidayName,
    bool IsTradingDay);
