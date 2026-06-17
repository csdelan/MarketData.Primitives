using MarketData.Primitives.Sessions;

namespace MarketData.Application.Contracts;

/// <summary>
/// Clock-aware view over an <see cref="IMarketCalendar"/>. Converts instants to phases and
/// builds rich <see cref="MarketContext"/> snapshots using an injected <see cref="TimeProvider"/>.
/// Venue-agnostic: it works with any calendar.
/// </summary>
public interface IMarketContextProvider
{
    /// <summary>The venue this provider describes.</summary>
    string VenueId { get; }

    /// <summary>The underlying calendar.</summary>
    IMarketCalendar Calendar { get; }

    /// <summary>Builds a context snapshot for the current instant from the injected clock.</summary>
    MarketContext GetContext();

    /// <summary>Builds a context snapshot for an explicit instant (deterministic core).</summary>
    MarketContext GetContextAt(DateTimeOffset instant);

    /// <summary>The phase active at an instant.</summary>
    MarketPhase GetActivePhase(DateTimeOffset instant);

    /// <summary>The liquidity level at an instant.</summary>
    SessionLiquidityLevel GetLiquidity(DateTimeOffset instant);

    /// <summary>Whether the regular session is open at an instant.</summary>
    bool IsRegularSessionOpen(DateTimeOffset instant);

    /// <summary>The next regular-session open at or after the instant; null when currently open.</summary>
    DateTimeOffset? NextRegularOpenUtc(DateTimeOffset instant);

    /// <summary>The regular-session close for the current session (if open) or the next session's close.</summary>
    DateTimeOffset? CurrentOrNextRegularCloseUtc(DateTimeOffset instant);
}
