using MarketData.Application.Contracts;
using MarketData.Primitives.Sessions;

namespace MarketData.Application.Calendar;

/// <summary>
/// Clock-aware provider that resolves instants to phases and builds <see cref="MarketContext"/>
/// snapshots over any <see cref="IMarketCalendar"/>. All phase boundaries are computed from
/// venue-local wall-clock times converted per-instant (DST-correct).
/// </summary>
public sealed class MarketContextProvider : IMarketContextProvider
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _tz;
    private readonly VenueSchedule _schedule;

    public MarketContextProvider(IMarketCalendar calendar, TimeProvider timeProvider)
    {
        Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _schedule = calendar.Schedule;
        _tz = MarketTimeZoneProvider.For(_schedule.TimeZoneId);
    }

    public string VenueId => Calendar.VenueId;
    public IMarketCalendar Calendar { get; }

    public MarketContext GetContext() => GetContextAt(_timeProvider.GetUtcNow());

    public MarketPhase GetActivePhase(DateTimeOffset instant)
    {
        var local = ToLocal(instant);
        var localDate = DateOnly.FromDateTime(local.DateTime);
        var localTime = TimeOnly.FromDateTime(local.DateTime);

        var window = Calendar.GetSessionWindow(localDate);
        if (window is not null)
        {
            if (instant >= window.RegularOpenUtc && instant < window.RegularCloseUtc)
                return MarketPhase.Regular;
            if (window.PreMarketOpenUtc is { } pre && instant >= pre && instant < window.RegularOpenUtc)
                return MarketPhase.PreMarket;
            if (window.PostMarketCloseUtc is { } postClose && instant >= window.RegularCloseUtc && instant < postClose)
                return MarketPhase.PostMarket;
        }

        if (IsOvernightOpen(localDate, localTime))
            return MarketPhase.OvernightFutures;

        return MarketPhase.Closed;
    }

    public SessionLiquidityLevel GetLiquidity(DateTimeOffset instant) => ToLiquidity(GetActivePhase(instant));

    public bool IsRegularSessionOpen(DateTimeOffset instant) => GetActivePhase(instant) == MarketPhase.Regular;

    public DateTimeOffset? NextRegularOpenUtc(DateTimeOffset instant)
    {
        if (IsRegularSessionOpen(instant))
            return null;

        var localDate = DateOnly.FromDateTime(ToLocal(instant).DateTime);
        var today = Calendar.GetSessionWindow(localDate);
        if (today is not null && instant < today.RegularOpenUtc)
            return today.RegularOpenUtc;

        var next = Calendar.NextTradingDay(localDate);
        return Calendar.GetSessionWindow(next)!.RegularOpenUtc;
    }

    public DateTimeOffset? CurrentOrNextRegularCloseUtc(DateTimeOffset instant)
    {
        var localDate = DateOnly.FromDateTime(ToLocal(instant).DateTime);
        var today = Calendar.GetSessionWindow(localDate);
        if (today is not null && instant < today.RegularCloseUtc)
            return today.RegularCloseUtc;

        var next = Calendar.NextTradingDay(localDate);
        return Calendar.GetSessionWindow(next)!.RegularCloseUtc;
    }

    public MarketContext GetContextAt(DateTimeOffset instant)
    {
        instant = instant.ToUniversalTime();
        var local = ToLocal(instant);
        var localDate = DateOnly.FromDateTime(local.DateTime);
        var localTime = TimeOnly.FromDateTime(local.DateTime);

        var phase = GetActivePhase(instant);
        var dayInfo = Calendar.ClassifyDay(localDate);

        // The trading date that owns this moment. During an evening overnight session the
        // wall-clock date is not the trading date the session leads into.
        DateOnly tradingDate = dayInfo.IsTradingDay
            ? localDate
            : (phase == MarketPhase.OvernightFutures && localTime >= _schedule.OvernightOpen)
                ? Calendar.NextTradingDay(localDate)
                : localDate;

        var window = Calendar.GetSessionWindow(tradingDate);

        bool regularOpen = phase == MarketPhase.Regular;
        TimeSpan? regularElapsed = null, regularRemaining = null, timeUntilOpen = null;
        double? regularProgress = null;
        DateTimeOffset? nextRegularOpen = null;
        if (regularOpen && window is not null)
        {
            regularElapsed = instant - window.RegularOpenUtc;
            regularRemaining = window.RegularCloseUtc - instant;
            var length = window.RegularCloseUtc - window.RegularOpenUtc;
            regularProgress = length > TimeSpan.Zero
                ? Math.Clamp((instant - window.RegularOpenUtc) / length, 0d, 1d)
                : null;
        }
        else
        {
            nextRegularOpen = NextRegularOpenUtc(instant);
            timeUntilOpen = nextRegularOpen - instant;
        }

        var ordinal = Calendar.GetTradingDayOrdinal(tradingDate);
        ComputePeriodEnds(tradingDate, dayInfo.IsTradingDay || phase == MarketPhase.OvernightFutures,
            out bool isWeekEnd, out bool isMonthEnd, out bool isQuarterEnd);

        var (nextTransition, nextPhase) = NextTransition(instant, phase);

        return new MarketContext
        {
            VenueId = VenueId,
            AsOfUtc = instant,
            AsOfVenueLocal = local,
            TradingDate = tradingDate,
            Day = dayInfo,
            Window = window,
            ActivePhase = phase,
            Liquidity = ToLiquidity(phase),
            IsRegularSessionOpen = regularOpen,
            RegularElapsed = regularElapsed,
            RegularRemaining = regularRemaining,
            RegularProgress = regularProgress,
            NextRegularOpenUtc = nextRegularOpen,
            TimeUntilNextRegularOpen = timeUntilOpen,
            Overnight = BuildOvernightPhase(instant),
            PreMarket = BuildEquityPhase(instant, MarketPhase.PreMarket,
                w => (w.PreMarketOpenUtc!.Value, w.RegularOpenUtc)),
            PostMarket = BuildEquityPhase(instant, MarketPhase.PostMarket,
                w => w.PostMarketCloseUtc is { } pc ? (w.RegularCloseUtc, pc) : null),
            NextTransitionUtc = nextTransition,
            NextPhase = nextPhase,
            Ordinal = ordinal,
            IsWeekEnd = isWeekEnd,
            IsMonthEnd = isMonthEnd,
            IsQuarterEnd = isQuarterEnd,
            NextMonthlyExpiration = Calendar.NextExpirationOnOrAfter(tradingDate, quarterlyOnly: false),
            NextQuarterlyExpiration = Calendar.NextExpirationOnOrAfter(tradingDate, quarterlyOnly: true),
        };
    }

    // --- phase status builders ---

    private PhaseStatus BuildEquityPhase(
        DateTimeOffset instant, MarketPhase phase,
        Func<MarketSessionWindow, (DateTimeOffset Start, DateTimeOffset End)?> extract)
    {
        var localDate = DateOnly.FromDateTime(ToLocal(instant).DateTime);
        var today = Calendar.GetSessionWindow(localDate);

        if (today is not null && extract(today) is { } cur)
        {
            if (instant >= cur.Start && instant < cur.End)
                return new PhaseStatus(phase, true, cur.Start, cur.End, instant - cur.Start, cur.End - instant, null);
            if (instant < cur.Start)
                return Inactive(phase, cur.Start, cur.End, instant);
        }

        var d = localDate;
        for (int i = 0; i < 12; i++)
        {
            d = Calendar.NextTradingDay(d);
            var w = Calendar.GetSessionWindow(d);
            if (w is not null && extract(w) is { } occ)
                return Inactive(phase, occ.Start, occ.End, instant);
        }
        return new PhaseStatus(phase, false, null, null, null, null, null);
    }

    private static PhaseStatus Inactive(MarketPhase phase, DateTimeOffset start, DateTimeOffset end, DateTimeOffset instant)
        => new(phase, false, start, end, null, null, start - instant);

    private PhaseStatus BuildOvernightPhase(DateTimeOffset instant)
    {
        if (!_schedule.HasOvernightFutures)
            return new PhaseStatus(MarketPhase.OvernightFutures, false, null, null, null, null, null);

        var local = ToLocal(instant);
        var date = DateOnly.FromDateTime(local.DateTime);
        var time = TimeOnly.FromDateTime(local.DateTime);

        if (IsOvernightOpen(date, time))
        {
            DateTimeOffset start, end;
            if (time >= _schedule.OvernightOpen)
            {
                start = ToUtc(date, _schedule.OvernightOpen);
                end = ToUtc(date.AddDays(1), _schedule.OvernightMaintenanceStart);
            }
            else
            {
                start = ToUtc(date.AddDays(-1), _schedule.OvernightOpen);
                end = ToUtc(date, _schedule.OvernightMaintenanceStart);
            }
            return new PhaseStatus(MarketPhase.OvernightFutures, true, start, end, instant - start, end - instant, null);
        }

        var next = NextOvernightOpen(instant);
        return new PhaseStatus(MarketPhase.OvernightFutures, false, next, null, null, null,
            next.HasValue ? next - instant : null);
    }

    // --- overnight rules ---

    private bool IsOvernightOpen(DateOnly date, TimeOnly time)
    {
        if (!_schedule.HasOvernightFutures)
            return false;

        var open = _schedule.OvernightOpen;
        var maint = _schedule.OvernightMaintenanceStart;
        bool windowOpen = date.DayOfWeek switch
        {
            DayOfWeek.Saturday => false,
            DayOfWeek.Sunday => time >= open,
            DayOfWeek.Friday => time < maint,
            _ => time < maint || time >= open,
        };
        if (!windowOpen)
            return false;

        var owning = time >= open ? date.AddDays(1) : date;
        return !IsFullClosure(owning);
    }

    private DateTimeOffset? NextOvernightOpen(DateTimeOffset instant)
    {
        if (!_schedule.HasOvernightFutures)
            return null;

        var date = DateOnly.FromDateTime(ToLocal(instant).DateTime);
        for (int i = 0; i <= 9; i++)
        {
            var d = date.AddDays(i);
            if (d.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
                continue;
            if (IsFullClosure(d.AddDays(1)))
                continue;
            var candidate = ToUtc(d, _schedule.OvernightOpen);
            if (candidate > instant)
                return candidate;
        }
        return null;
    }

    // --- transitions ---

    private (DateTimeOffset? Transition, MarketPhase? Phase) NextTransition(DateTimeOffset instant, MarketPhase current)
    {
        var candidates = new SortedSet<DateTimeOffset>();
        var startDate = DateOnly.FromDateTime(ToLocal(instant).DateTime).AddDays(-1);
        for (int i = 0; i <= 7; i++)
        {
            var d = startDate.AddDays(i);
            candidates.Add(ToUtc(d, _schedule.OvernightOpen));
            candidates.Add(ToUtc(d, _schedule.OvernightMaintenanceStart));
            var w = Calendar.GetSessionWindow(d);
            if (w is not null)
            {
                if (w.PreMarketOpenUtc is { } pre) candidates.Add(pre);
                candidates.Add(w.RegularOpenUtc);
                candidates.Add(w.RegularCloseUtc);
                if (w.PostMarketCloseUtc is { } pc) candidates.Add(pc);
            }
        }

        foreach (var c in candidates)
        {
            if (c <= instant)
                continue;
            var phaseAt = GetActivePhase(c);
            if (phaseAt != current)
                return (c, phaseAt);
        }
        return (null, null);
    }

    // --- period-end flags ---

    private void ComputePeriodEnds(DateOnly tradingDate, bool meaningful,
        out bool isWeekEnd, out bool isMonthEnd, out bool isQuarterEnd)
    {
        isWeekEnd = isMonthEnd = isQuarterEnd = false;
        if (!meaningful || !Calendar.IsTradingDay(tradingDate))
            return;

        var next = Calendar.NextTradingDay(tradingDate);
        int daysFromMonday = ((int)tradingDate.DayOfWeek + 6) % 7;
        var weekEnd = tradingDate.AddDays(6 - daysFromMonday);
        isWeekEnd = next > weekEnd;
        isMonthEnd = next.Month != tradingDate.Month || next.Year != tradingDate.Year;
        isQuarterEnd = Quarter(next) != Quarter(tradingDate) || next.Year != tradingDate.Year;
    }

    private static int Quarter(DateOnly date) => (date.Month - 1) / 3 + 1;

    // --- helpers ---

    private bool IsFullClosure(DateOnly date)
        => Calendar.TryGetHoliday(date, out var h) && !h.IsEarlyClose;

    private static SessionLiquidityLevel ToLiquidity(MarketPhase phase) => phase switch
    {
        MarketPhase.Regular => SessionLiquidityLevel.Full,
        MarketPhase.PreMarket or MarketPhase.PostMarket => SessionLiquidityLevel.Reduced,
        MarketPhase.OvernightFutures => SessionLiquidityLevel.Light,
        _ => SessionLiquidityLevel.None,
    };

    private DateTimeOffset ToLocal(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, _tz);

    private DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var localDateTime = date.ToDateTime(time);
        var offset = _tz.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }
}
