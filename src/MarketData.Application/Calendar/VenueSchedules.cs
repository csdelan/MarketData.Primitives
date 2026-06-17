using MarketData.Primitives.Sessions;

namespace MarketData.Application.Calendar;

/// <summary>
/// Factory for built-in <see cref="VenueSchedule"/> definitions. Adding a venue is a matter
/// of adding a factory here plus a holiday-rules source — the engine is otherwise unchanged.
/// </summary>
public static class VenueSchedules
{
    /// <summary>
    /// The composite US-equity venue: equities pre/regular/post (NYSE hours) plus an
    /// overnight equity-index futures session (CME Globex ES style). One schedule that
    /// spans the full 24-hour US-equity ecosystem.
    /// </summary>
    public static VenueSchedule UsEquityComposite() => new(
        venueId: "US-EQ",
        timeZoneId: "America/New_York",
        regularOpen: new TimeOnly(9, 30),
        regularClose: new TimeOnly(16, 0),
        halfDayClose: new TimeOnly(13, 0),
        preMarketOpen: new TimeOnly(4, 0),
        postMarketClose: new TimeOnly(20, 0),
        hasOvernightFutures: true,
        overnightOpen: new TimeOnly(18, 0),
        overnightMaintenanceStart: new TimeOnly(17, 0),
        postMarketOnHalfDays: false);
}
