using MarketData.Application.Calendar;
using MarketData.Application.Contracts;
using MarketData.Primitives.Sessions;

namespace MarketData.Application.Services;

/// <summary>
/// Synchronous, convenience facade over an <see cref="IMarketContextProvider"/>. Replaces the
/// former <c>MarketHoursProvider</c>. The entire stack is deterministic and synchronous, so
/// there is no sync-over-async anywhere.
/// </summary>
public sealed class MarketClock
{
    private readonly IMarketContextProvider _provider;
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _tz;

    public MarketClock(IMarketContextProvider provider, TimeProvider timeProvider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tz = MarketTimeZoneProvider.For(provider.Calendar.Schedule.TimeZoneId);
    }

    /// <summary>The underlying calendar.</summary>
    public IMarketCalendar Calendar => _provider.Calendar;

    /// <summary>Whether the regular session is open right now.</summary>
    public bool IsMarketOpen() => _provider.IsRegularSessionOpen(Now());

    /// <summary>Whether the regular session is open at the given instant.</summary>
    public bool IsMarketOpen(DateTimeOffset instant) => _provider.IsRegularSessionOpen(instant);

    /// <summary>The current liquidity level.</summary>
    public SessionLiquidityLevel Liquidity() => _provider.GetLiquidity(Now());

    /// <summary>A full context snapshot for the current instant.</summary>
    public MarketContext Context() => _provider.GetContext();

    /// <summary>The next regular-session open after now (skips weekends and holidays).</summary>
    public DateTimeOffset NextMarketOpen()
    {
        var now = Now();
        var next = _provider.NextRegularOpenUtc(now);
        if (next.HasValue)
            return next.Value;

        // The session is open right now: the next open is the next trading day's open.
        var localDate = DateOnly.FromDateTime(ToLocal(now).DateTime);
        var nextDay = Calendar.NextTradingDay(localDate);
        return Calendar.GetSessionWindow(nextDay)!.RegularOpenUtc;
    }

    /// <summary>Today's regular-session close (UTC), or null if today is not a trading day.</summary>
    public DateTimeOffset? TodayCloseUtc()
    {
        var localDate = DateOnly.FromDateTime(ToLocal(Now()).DateTime);
        return Calendar.GetSessionWindow(localDate)?.RegularCloseUtc;
    }

    /// <summary>Classifies a date.</summary>
    public MarketDayInfo Classify(DateOnly date) => Calendar.ClassifyDay(date);

    /// <summary>Trading days from today (exclusive) until the target date (inclusive).</summary>
    public int TradingDaysUntil(DateOnly target)
    {
        var today = DateOnly.FromDateTime(ToLocal(Now()).DateTime);
        return Calendar.CountTradingDays(today.AddDays(1), target);
    }

    private DateTimeOffset Now() => _timeProvider.GetUtcNow();

    private DateTimeOffset ToLocal(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, _tz);
}
