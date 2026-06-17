namespace MarketData.Workers;

/// <summary>
/// How a job's next-fire instant is computed. Market-relative triggers are resolved against the
/// venue calendar (honouring weekends, holidays and half-days), which a plain cron cannot express.
/// </summary>
public enum ScheduleTrigger
{
    /// <summary>Fire every <c>IntervalMinutes</c> regardless of market state (clock-independent demo trigger).</summary>
    IntervalAlways,

    /// <summary>Fire at the next regular-session open.</summary>
    MarketOpen,

    /// <summary>Fire at the current or next regular-session close.</summary>
    MarketClose,

    /// <summary>Fire on an <c>IntervalMinutes</c> grid, but only while the regular session is open (the "5m candle" trigger).</summary>
    EveryNMinutesDuringMarketHours,

    /// <summary>Delegate to a Hangfire recurring job using <c>Cron</c>.</summary>
    Cron,
}
