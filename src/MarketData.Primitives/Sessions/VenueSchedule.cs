using Core;

namespace MarketData.Primitives.Sessions;

/// <summary>
/// Immutable description of a venue's intraday phase layout. Pure data — no clock,
/// no timezone conversion. All times are venue-local wall-clock <see cref="TimeOnly"/>.
/// This is the primary seam for adding new venues without rewriting the engine.
/// </summary>
public sealed class VenueSchedule : ValueObject
{
    /// <summary>Stable venue identifier (e.g., "US-EQ", "NYSE", "LSE").</summary>
    public string VenueId { get; }

    /// <summary>Timezone id used to resolve the venue's local wall clock to UTC.</summary>
    public string TimeZoneId { get; }

    /// <summary>Regular session open (e.g., 09:30).</summary>
    public TimeOnly RegularOpen { get; }

    /// <summary>Regular session close on a full day (e.g., 16:00).</summary>
    public TimeOnly RegularClose { get; }

    /// <summary>Regular session close on an early-close (half) day (e.g., 13:00).</summary>
    public TimeOnly HalfDayClose { get; }

    /// <summary>Pre-market open (e.g., 04:00).</summary>
    public TimeOnly PreMarketOpen { get; }

    /// <summary>Post-market close (e.g., 20:00).</summary>
    public TimeOnly PostMarketClose { get; }

    /// <summary>Whether this venue runs an overnight futures session.</summary>
    public bool HasOvernightFutures { get; }

    /// <summary>Overnight session open / daily reopen after maintenance (e.g., 18:00).</summary>
    public TimeOnly OvernightOpen { get; }

    /// <summary>Start of the daily overnight maintenance halt (e.g., 17:00).</summary>
    public TimeOnly OvernightMaintenanceStart { get; }

    /// <summary>Whether the post-market session runs on early-close days. Recommended false.</summary>
    public bool PostMarketOnHalfDays { get; }

    public VenueSchedule(
        string venueId,
        string timeZoneId,
        TimeOnly regularOpen,
        TimeOnly regularClose,
        TimeOnly halfDayClose,
        TimeOnly preMarketOpen,
        TimeOnly postMarketClose,
        bool hasOvernightFutures = false,
        TimeOnly overnightOpen = default,
        TimeOnly overnightMaintenanceStart = default,
        bool postMarketOnHalfDays = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(venueId);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);

        VenueId = venueId;
        TimeZoneId = timeZoneId;
        RegularOpen = regularOpen;
        RegularClose = regularClose;
        HalfDayClose = halfDayClose;
        PreMarketOpen = preMarketOpen;
        PostMarketClose = postMarketClose;
        HasOvernightFutures = hasOvernightFutures;
        OvernightOpen = overnightOpen;
        OvernightMaintenanceStart = overnightMaintenanceStart;
        PostMarketOnHalfDays = postMarketOnHalfDays;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return VenueId;
        yield return TimeZoneId;
        yield return RegularOpen;
        yield return RegularClose;
        yield return HalfDayClose;
        yield return PreMarketOpen;
        yield return PostMarketClose;
        yield return HasOvernightFutures;
        yield return OvernightOpen;
        yield return OvernightMaintenanceStart;
        yield return PostMarketOnHalfDays;
    }
}
