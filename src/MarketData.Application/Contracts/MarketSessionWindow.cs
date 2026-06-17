using MarketData.Primitives.Sessions;

namespace MarketData.Application.Contracts;

/// <summary>
/// A concrete trading date's phase windows resolved to UTC instants. Nullable members
/// are absent for the relevant day (e.g., no post-market on a half day, no overnight
/// when the venue has none or the following session is suppressed).
/// </summary>
/// <param name="TradingDate">The trading date these windows belong to.</param>
/// <param name="Kind">The day classification (RegularDay or HalfDay; windows are only built for trading days).</param>
/// <param name="PreMarketOpenUtc">Pre-market open instant.</param>
/// <param name="RegularOpenUtc">Regular session open instant.</param>
/// <param name="RegularCloseUtc">Regular session close instant (13:00 ET on half days).</param>
/// <param name="PostMarketCloseUtc">Post-market close instant; null on half days.</param>
/// <param name="OvernightOpenUtc">Open instant of the overnight session that leads into this trading date; null if none.</param>
/// <param name="OvernightMaintenanceStartUtc">Start of the daily maintenance halt for this date's overnight; null if none.</param>
public sealed record MarketSessionWindow(
    DateOnly TradingDate,
    MarketDayKind Kind,
    DateTimeOffset? PreMarketOpenUtc,
    DateTimeOffset RegularOpenUtc,
    DateTimeOffset RegularCloseUtc,
    DateTimeOffset? PostMarketCloseUtc,
    DateTimeOffset? OvernightOpenUtc,
    DateTimeOffset? OvernightMaintenanceStartUtc);
