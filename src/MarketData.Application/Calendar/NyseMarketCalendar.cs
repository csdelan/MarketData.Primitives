using System.Globalization;
using MarketData.Application.Contracts;
using MarketData.Primitives;
using MarketData.Primitives.Sessions;

namespace MarketData.Application.Calendar;

/// <summary>
/// Deterministic US-equity market calendar (NYSE rules + composite US-equity schedule).
/// Pure date logic with no clock. Holiday data is composed, in precedence order, from
/// computed rules, the bundled special-closure table, and per-year JSON overrides.
/// </summary>
public sealed class NyseMarketCalendar : IMarketCalendar
{
    private readonly HolidayOverrideLoader _overrides;
    private readonly TimeZoneInfo _tz;
    private readonly Dictionary<int, YearData> _cache = [];

    public NyseMarketCalendar(VenueSchedule? schedule = null, HolidayOverrideLoader? overrides = null)
    {
        Schedule = schedule ?? VenueSchedules.UsEquityComposite();
        _overrides = overrides ?? new HolidayOverrideLoader();
        _tz = MarketTimeZoneProvider.For(Schedule.TimeZoneId);
    }

    public string VenueId => Schedule.VenueId;
    public VenueSchedule Schedule { get; }

    // --- classification ---

    public bool IsTradingDay(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;
        return !IsFullClosure(date);
    }

    public MarketDayInfo ClassifyDay(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return new MarketDayInfo(date, MarketDayKind.Weekend, null, IsTradingDay: false);

        if (TryGetHoliday(date, out var holiday))
        {
            return holiday.IsEarlyClose
                ? new MarketDayInfo(date, MarketDayKind.HalfDay, holiday.Name, IsTradingDay: true)
                : new MarketDayInfo(date, MarketDayKind.Holiday, holiday.Name, IsTradingDay: false);
        }

        return new MarketDayInfo(date, MarketDayKind.RegularDay, null, IsTradingDay: true);
    }

    public MarketSessionWindow? GetSessionWindow(DateOnly date)
    {
        var info = ClassifyDay(date);
        if (!info.IsTradingDay)
            return null;

        bool isHalfDay = info.Kind == MarketDayKind.HalfDay;
        var close = isHalfDay ? Schedule.HalfDayClose : Schedule.RegularClose;

        DateTimeOffset? postClose =
            (isHalfDay && !Schedule.PostMarketOnHalfDays) ? null : ToUtc(date, Schedule.PostMarketClose);

        DateTimeOffset? overnightOpen = null;
        DateTimeOffset? overnightMaint = null;
        if (Schedule.HasOvernightFutures)
        {
            // The overnight session leading into this trading date opens the prior evening
            // and ends at the maintenance halt on this date. Suppressed if the prior
            // calendar day is a full closure (documented simplification: NYSE calendar).
            var priorDay = date.AddDays(-1);
            if (!IsFullClosure(priorDay))
            {
                overnightOpen = ToUtc(priorDay, Schedule.OvernightOpen);
                overnightMaint = ToUtc(date, Schedule.OvernightMaintenanceStart);
            }
        }

        return new MarketSessionWindow(
            TradingDate: date,
            Kind: info.Kind,
            PreMarketOpenUtc: ToUtc(date, Schedule.PreMarketOpen),
            RegularOpenUtc: ToUtc(date, Schedule.RegularOpen),
            RegularCloseUtc: ToUtc(date, close),
            PostMarketCloseUtc: postClose,
            OvernightOpenUtc: overnightOpen,
            OvernightMaintenanceStartUtc: overnightMaint);
    }

    // --- holidays ---

    public MarketHolidayCalendarYear GetCalendarYear(int year) => GetYearData(year).Calendar;

    public bool TryGetHoliday(DateOnly date, out Holiday holiday)
        => GetYearData(date.Year).ByDate.TryGetValue(date, out holiday!);

    private bool IsFullClosure(DateOnly date)
        => GetYearData(date.Year).ByDate.TryGetValue(date, out var h) && !h.IsEarlyClose;

    // --- navigation ---

    public DateOnly NextTradingDay(DateOnly date)
    {
        do { date = date.AddDays(1); } while (!IsTradingDay(date));
        return date;
    }

    public DateOnly PreviousTradingDay(DateOnly date)
    {
        do { date = date.AddDays(-1); } while (!IsTradingDay(date));
        return date;
    }

    public DateOnly AddTradingDays(DateOnly start, int count)
    {
        if (count == 0) return start;
        int step = Math.Sign(count);
        int remaining = Math.Abs(count);
        var date = start;
        while (remaining > 0)
        {
            date = date.AddDays(step);
            if (IsTradingDay(date)) remaining--;
        }
        return date;
    }

    // --- counting / numbering ---

    public int CountTradingDays(DateOnly startInclusive, DateOnly endInclusive)
    {
        if (startInclusive > endInclusive)
            return -CountTradingDays(endInclusive, startInclusive);

        int count = 0;
        for (var d = startInclusive; d <= endInclusive; d = d.AddDays(1))
            if (IsTradingDay(d)) count++;
        return count;
    }

    public int IsoWeekNumber(DateOnly date)
        => ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));

    public TradingDayOrdinal GetTradingDayOrdinal(DateOnly date)
    {
        if (!IsTradingDay(date))
            return new TradingDayOrdinal(0, 0, 0, 0);

        int daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        var weekStart = date.AddDays(-daysFromMonday);
        var monthStart = new DateOnly(date.Year, date.Month, 1);
        int quarterFirstMonth = ((date.Month - 1) / 3) * 3 + 1;
        var quarterStart = new DateOnly(date.Year, quarterFirstMonth, 1);
        var yearStart = new DateOnly(date.Year, 1, 1);

        return new TradingDayOrdinal(
            OfWeek: CountTradingDays(weekStart, date),
            OfMonth: CountTradingDays(monthStart, date),
            OfQuarter: CountTradingDays(quarterStart, date),
            OfYear: CountTradingDays(yearStart, date));
    }

    public TradingPeriodStats GetPeriodStats(DateOnly reference, ResolutionUnit period)
    {
        var (start, end) = PeriodBounds(reference, period);
        int total = CountTradingDays(start, end);
        int elapsed = CountTradingDays(start, reference);
        return new TradingPeriodStats(period, start, end, total, elapsed, total - elapsed);
    }

    private static (DateOnly Start, DateOnly End) PeriodBounds(DateOnly reference, ResolutionUnit period)
    {
        switch (period)
        {
            case ResolutionUnit.Weeks:
                int daysFromMonday = ((int)reference.DayOfWeek + 6) % 7;
                var weekStart = reference.AddDays(-daysFromMonday);
                return (weekStart, weekStart.AddDays(6));
            case ResolutionUnit.Months:
                var monthStart = new DateOnly(reference.Year, reference.Month, 1);
                return (monthStart, monthStart.AddMonths(1).AddDays(-1));
            case ResolutionUnit.Quarters:
                int qFirstMonth = ((reference.Month - 1) / 3) * 3 + 1;
                var qStart = new DateOnly(reference.Year, qFirstMonth, 1);
                return (qStart, qStart.AddMonths(3).AddDays(-1));
            case ResolutionUnit.Years:
                return (new DateOnly(reference.Year, 1, 1), new DateOnly(reference.Year, 12, 31));
            default:
                throw new ArgumentOutOfRangeException(nameof(period), period,
                    "Period stats support Weeks, Months, Quarters, and Years.");
        }
    }

    // --- options expiration / witching ---

    public OptionsExpiration GetMonthlyExpiration(int year, int month)
    {
        var thirdFriday = UsEquityHolidayRules.NthWeekday(year, month, DayOfWeek.Friday, 3);
        var adjusted = thirdFriday;
        while (IsFullClosure(adjusted))
            adjusted = adjusted.AddDays(-1);

        bool isQuarterly = month is 3 or 6 or 9 or 12;
        var witching = isQuarterly ? WitchingKind.QuadWitching : WitchingKind.None;
        return new OptionsExpiration(adjusted, thirdFriday, isQuarterly, witching);
    }

    public OptionsExpiration GetQuarterlyExpiration(int year, int quarter)
    {
        if (quarter is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(quarter), quarter, "Quarter must be 1..4.");
        return GetMonthlyExpiration(year, quarter * 3);
    }

    public IReadOnlyList<OptionsExpiration> GetWitchingDates(int year) =>
    [
        GetQuarterlyExpiration(year, 1),
        GetQuarterlyExpiration(year, 2),
        GetQuarterlyExpiration(year, 3),
        GetQuarterlyExpiration(year, 4),
    ];

    public OptionsExpiration NextExpirationOnOrAfter(DateOnly date, bool quarterlyOnly = false)
    {
        int year = date.Year;
        int month = date.Month;
        for (int i = 0; i < 24; i++)
        {
            var expiration = GetMonthlyExpiration(year, month);
            if (expiration.Date >= date && (!quarterlyOnly || expiration.IsQuarterly))
                return expiration;

            if (++month > 12) { month = 1; year++; }
        }
        throw new InvalidOperationException("No expiration found within 24 months.");
    }

    public DateOnly SettlementDate(DateOnly tradeDate, int tradingDays = 1)
        => AddTradingDays(tradeDate, tradingDays);

    // --- helpers ---

    private DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);                 // DateTimeKind.Unspecified
        var offset = _tz.GetUtcOffset(local);              // offset at this wall-clock time (DST-correct)
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    private YearData GetYearData(int year)
    {
        if (_cache.TryGetValue(year, out var cached))
            return cached;

        var map = new Dictionary<DateOnly, Holiday>();
        void Apply(IEnumerable<Holiday> entries)
        {
            foreach (var h in entries) map[h.Date] = h;
        }

        // Precedence (later wins; within a tier, full closures applied after early closes
        // so an observed full holiday beats a fixed-date early close on the same day):
        Apply(UsEquityHolidayRules.EarlyCloses(year));
        Apply(UsEquityHolidayRules.Holidays(year));
        Apply(UsEquityClosures.EarlyCloses(year));
        Apply(UsEquityClosures.Closures(year));
        var ov = _overrides.Load(year);
        if (ov is not null)
        {
            Apply(ov.EarlyCloses);
            Apply(ov.Holidays);
        }

        var holidays = map.Values.Where(h => !h.IsEarlyClose).OrderBy(h => h.Date).ToList();
        var earlyCloses = map.Values.Where(h => h.IsEarlyClose).OrderBy(h => h.Date).ToList();
        var data = new YearData(
            new MarketHolidayCalendarYear(year, VenueId, holidays, earlyCloses),
            map);

        _cache[year] = data;
        return data;
    }

    private sealed record YearData(MarketHolidayCalendarYear Calendar, IReadOnlyDictionary<DateOnly, Holiday> ByDate);
}
