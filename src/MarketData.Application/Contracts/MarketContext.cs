using MarketData.Primitives.Sessions;

namespace MarketData.Application.Contracts;

/// <summary>
/// A rich, single-call snapshot of the market state at an instant. Composed from one
/// session-window resolution plus a couple of expiration computations, so every field
/// is cheap. The granular methods on <see cref="IMarketContextProvider"/> and
/// <see cref="IMarketCalendar"/> remain the primary API; this is a convenience aggregate
/// that avoids multiple round-trips.
/// </summary>
public sealed record MarketContext
{
    /// <summary>The venue this context describes.</summary>
    public required string VenueId { get; init; }

    /// <summary>The instant this snapshot was taken (UTC).</summary>
    public required DateTimeOffset AsOfUtc { get; init; }

    /// <summary>The same instant expressed in the venue's local time.</summary>
    public required DateTimeOffset AsOfVenueLocal { get; init; }

    /// <summary>
    /// The trading date that owns the current moment. During an overnight session this is
    /// the date the session leads into, which may differ from the wall-clock date.
    /// </summary>
    public required DateOnly TradingDate { get; init; }

    /// <summary>Classification of the wall-clock calendar date.</summary>
    public required MarketDayInfo Day { get; init; }

    /// <summary>Resolved session windows for the trading date; null on non-trading days with no overnight.</summary>
    public MarketSessionWindow? Window { get; init; }

    /// <summary>The phase currently active.</summary>
    public required MarketPhase ActivePhase { get; init; }

    /// <summary>Liquidity level derived from the active phase.</summary>
    public required SessionLiquidityLevel Liquidity { get; init; }

    /// <summary>Whether the regular session is open right now.</summary>
    public required bool IsRegularSessionOpen { get; init; }

    /// <summary>Time since the regular session opened; null when not open.</summary>
    public TimeSpan? RegularElapsed { get; init; }

    /// <summary>Time until the regular session closes; null when not open.</summary>
    public TimeSpan? RegularRemaining { get; init; }

    /// <summary>Fraction (0..1) of the regular session elapsed; null when not open.</summary>
    public double? RegularProgress { get; init; }

    /// <summary>The next regular-session open instant; null when the session is currently open.</summary>
    public DateTimeOffset? NextRegularOpenUtc { get; init; }

    /// <summary>Time until the next regular open; null when the session is currently open.</summary>
    public TimeSpan? TimeUntilNextRegularOpen { get; init; }

    /// <summary>Status of the overnight futures phase.</summary>
    public required PhaseStatus Overnight { get; init; }

    /// <summary>Status of the pre-market phase.</summary>
    public required PhaseStatus PreMarket { get; init; }

    /// <summary>Status of the post-market phase.</summary>
    public required PhaseStatus PostMarket { get; init; }

    /// <summary>Instant of the next phase transition (start of <see cref="NextPhase"/>); null if none is determinable.</summary>
    public DateTimeOffset? NextTransitionUtc { get; init; }

    /// <summary>The phase that begins at <see cref="NextTransitionUtc"/>.</summary>
    public MarketPhase? NextPhase { get; init; }

    /// <summary>Trading-day ordinals of the trading date within week/month/quarter/year.</summary>
    public required TradingDayOrdinal Ordinal { get; init; }

    /// <summary>True if the trading date is the last trading day of its ISO week.</summary>
    public bool IsWeekEnd { get; init; }

    /// <summary>True if the trading date is the last trading day of its calendar month.</summary>
    public bool IsMonthEnd { get; init; }

    /// <summary>True if the trading date is the last trading day of its calendar quarter.</summary>
    public bool IsQuarterEnd { get; init; }

    /// <summary>The next monthly options expiration on or after the trading date.</summary>
    public OptionsExpiration? NextMonthlyExpiration { get; init; }

    /// <summary>The next quarterly options expiration on or after the trading date.</summary>
    public OptionsExpiration? NextQuarterlyExpiration { get; init; }
}
