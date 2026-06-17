namespace MarketData.Application.Contracts;

/// <summary>
/// The resolved holiday calendar for a single year at a venue: full closures and
/// named early-close days.
/// </summary>
public sealed record MarketHolidayCalendarYear(
    int Year,
    string VenueId,
    IReadOnlyList<Holiday> Holidays,
    IReadOnlyList<Holiday> EarlyCloses);
