namespace MarketData.Primitives.Sessions;

/// <summary>
/// A phase (era) within the 24-hour trading cycle of a venue. Ordered by the
/// normal progression through a trading day.
/// </summary>
public enum MarketPhase
{
    /// <summary>No phase is active (weekend gap, holiday, or the daily maintenance halt).</summary>
    Closed = 0,

    /// <summary>Overnight futures session (e.g., CME Globex equity-index), light liquidity.</summary>
    OvernightFutures,

    /// <summary>Pre-market equities session.</summary>
    PreMarket,

    /// <summary>Regular (primary) trading session.</summary>
    Regular,

    /// <summary>Post-market (after-hours) equities session.</summary>
    PostMarket
}
