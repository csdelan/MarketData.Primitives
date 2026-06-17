using MarketData.Application.Calendar;
using MarketData.Application.Contracts;
using MarketData.Primitives;
using MarketData.Primitives.Sessions;

namespace MarketData.Primitives.Tests;

public class MarketCalendarTests
{
    // A calendar whose JSON-override path is guaranteed not to exist, so tests are
    // deterministic regardless of the developer's local config.
    private static NyseMarketCalendar Calendar() =>
        new(overrides: new HolidayOverrideLoader(
            Path.Combine(Path.GetTempPath(), "marketdata-tests-" + Guid.NewGuid().ToString("N"))));

    // --- classification ---

    [Fact]
    public void ClassifyDay_RegularMonday_ReturnsRegularDay()
    {
        var info = Calendar().ClassifyDay(new DateOnly(2025, 5, 19));
        Assert.Equal(MarketDayKind.RegularDay, info.Kind);
        Assert.True(info.IsTradingDay);
        Assert.Null(info.HolidayName);
    }

    [Fact]
    public void ClassifyDay_Saturday_ReturnsWeekend()
    {
        var info = Calendar().ClassifyDay(new DateOnly(2025, 5, 17));
        Assert.Equal(MarketDayKind.Weekend, info.Kind);
        Assert.False(info.IsTradingDay);
    }

    [Fact]
    public void ClassifyDay_Christmas2025_ReturnsHolidayWithName()
    {
        var info = Calendar().ClassifyDay(new DateOnly(2025, 12, 25));
        Assert.Equal(MarketDayKind.Holiday, info.Kind);
        Assert.False(info.IsTradingDay);
        Assert.Equal("Christmas Day", info.HolidayName);
    }

    [Fact]
    public void ClassifyDay_GoodFriday2025_ReturnsHoliday()
    {
        var info = Calendar().ClassifyDay(new DateOnly(2025, 4, 18));
        Assert.Equal(MarketDayKind.Holiday, info.Kind);
        Assert.Equal("Good Friday", info.HolidayName);
    }

    [Fact]
    public void ClassifyDay_JulyFourthObservedFriday2026_ReturnsHoliday()
    {
        // 2026-07-04 is a Saturday → observed Friday 2026-07-03.
        var info = Calendar().ClassifyDay(new DateOnly(2026, 7, 3));
        Assert.Equal(MarketDayKind.Holiday, info.Kind);
        Assert.Equal("Independence Day", info.HolidayName);
    }

    [Fact]
    public void ClassifyDay_DayAfterThanksgiving2025_ReturnsHalfDayWithName()
    {
        var info = Calendar().ClassifyDay(new DateOnly(2025, 11, 28));
        Assert.Equal(MarketDayKind.HalfDay, info.Kind);
        Assert.True(info.IsTradingDay);
        Assert.Equal("Day after Thanksgiving", info.HolidayName);
    }

    [Fact]
    public void ClassifyDay_CarterMourning_ReturnsBundledHoliday()
    {
        var info = Calendar().ClassifyDay(new DateOnly(2025, 1, 9));
        Assert.Equal(MarketDayKind.Holiday, info.Kind);
        Assert.Contains("Carter", info.HolidayName);
    }

    [Theory]
    [InlineData(2012, 10, 29)]
    [InlineData(2012, 10, 30)]
    public void ClassifyDay_HurricaneSandy_ReturnsBundledHoliday(int y, int m, int d)
    {
        var info = Calendar().ClassifyDay(new DateOnly(y, m, d));
        Assert.Equal(MarketDayKind.Holiday, info.Kind);
        Assert.Contains("Sandy", info.HolidayName);
    }

    [Fact]
    public void ClassifyDay_Bush41Mourning_ReturnsBundledHoliday()
    {
        var info = Calendar().ClassifyDay(new DateOnly(2018, 12, 5));
        Assert.Equal(MarketDayKind.Holiday, info.Kind);
    }

    [Fact]
    public void GetCalendarYear_2025_IncludesJuneteenthAndNamedClosures()
    {
        var year = Calendar().GetCalendarYear(2025);
        Assert.Contains(year.Holidays, h => h.Name.StartsWith("Juneteenth"));
        Assert.All(year.Holidays, h => Assert.False(h.IsEarlyClose));
        Assert.All(year.EarlyCloses, h => Assert.True(h.IsEarlyClose));
    }

    // --- JSON overrides ---

    [Fact]
    public void GetCalendarYear_JsonOverrideWithName_TakesPrecedence()
    {
        using var dir = new TempConfig();
        dir.WriteHolidays(2099, """
            { "holidays": [ { "date": "2099-07-06", "name": "Custom Closure" } ] }
            """);
        var cal = new NyseMarketCalendar(overrides: new HolidayOverrideLoader(dir.Path));

        Assert.True(cal.TryGetHoliday(new DateOnly(2099, 7, 6), out var h));
        Assert.Equal("Custom Closure", h.Name);
        Assert.False(cal.IsTradingDay(new DateOnly(2099, 7, 6)));
    }

    [Fact]
    public void GetCalendarYear_LegacyBareDateJson_SynthesizesName()
    {
        using var dir = new TempConfig();
        dir.WriteHolidays(2099, """
            { "holidays": [ "2099-03-02" ], "halfDays": [ "2099-03-03" ] }
            """);
        var cal = new NyseMarketCalendar(overrides: new HolidayOverrideLoader(dir.Path));

        Assert.True(cal.TryGetHoliday(new DateOnly(2099, 3, 2), out var full));
        Assert.False(full.IsEarlyClose);
        Assert.True(cal.TryGetHoliday(new DateOnly(2099, 3, 3), out var early));
        Assert.True(early.IsEarlyClose);
    }

    // --- counting / numbering ---

    [Fact]
    public void CountTradingDays_FullWeekNoHolidays_Returns5()
    {
        Assert.Equal(5, Calendar().CountTradingDays(new DateOnly(2025, 5, 19), new DateOnly(2025, 5, 23)));
    }

    [Fact]
    public void CountTradingDays_WeekWithJulyFourth_Returns4()
    {
        // 2025-07-04 (Fri) is a holiday.
        Assert.Equal(4, Calendar().CountTradingDays(new DateOnly(2025, 6, 30), new DateOnly(2025, 7, 4)));
    }

    [Fact]
    public void CountTradingDays_ReversedRange_ReturnsNegative()
    {
        Assert.Equal(-5, Calendar().CountTradingDays(new DateOnly(2025, 5, 23), new DateOnly(2025, 5, 19)));
    }

    [Fact]
    public void IsoWeekNumber_FirstWeekOf2025_Returns1()
    {
        Assert.Equal(1, Calendar().IsoWeekNumber(new DateOnly(2025, 1, 1)));
    }

    [Fact]
    public void GetTradingDayOrdinal_ThirdTradingDayOfWeek_OfWeekIs3()
    {
        var ord = Calendar().GetTradingDayOrdinal(new DateOnly(2025, 5, 21)); // Wed of a full week
        Assert.Equal(3, ord.OfWeek);
    }

    [Fact]
    public void GetTradingDayOrdinal_OnWeekend_ReturnsZeros()
    {
        var ord = Calendar().GetTradingDayOrdinal(new DateOnly(2025, 5, 17));
        Assert.Equal(new TradingDayOrdinal(0, 0, 0, 0), ord);
    }

    [Fact]
    public void GetPeriodStats_Month_ElapsedInclusiveOfReference()
    {
        var cal = Calendar();
        var stats = cal.GetPeriodStats(new DateOnly(2025, 5, 19), ResolutionUnit.Months);
        Assert.Equal(cal.CountTradingDays(new DateOnly(2025, 5, 1), new DateOnly(2025, 5, 19)), stats.Elapsed);
        Assert.Equal(stats.Total, stats.Elapsed + stats.Remaining);
    }

    [Fact]
    public void GetPeriodStats_LastTradingDayOfQuarter_RemainingZero()
    {
        // Q2 2025 ends 2025-06-30 (Mon), a trading day and the last of the quarter.
        var stats = Calendar().GetPeriodStats(new DateOnly(2025, 6, 30), ResolutionUnit.Quarters);
        Assert.Equal(0, stats.Remaining);
    }

    [Fact]
    public void AddTradingDays_AcrossHolidayAndWeekend_SkipsCorrectly()
    {
        // 2025-07-03 (Thu) + 1 trading day: skip 07-04 holiday and the weekend → 07-07 (Mon).
        Assert.Equal(new DateOnly(2025, 7, 7), Calendar().AddTradingDays(new DateOnly(2025, 7, 3), 1));
    }

    [Fact]
    public void SettlementDate_DefaultTPlusOne_IsNextTradingDay()
    {
        Assert.Equal(new DateOnly(2025, 5, 20), Calendar().SettlementDate(new DateOnly(2025, 5, 19)));
    }

    // --- options expiration / witching ---

    [Fact]
    public void GetMonthlyExpiration_NormalMonth_IsThirdFriday()
    {
        var exp = Calendar().GetMonthlyExpiration(2025, 5);
        Assert.Equal(new DateOnly(2025, 5, 16), exp.Date);
        Assert.Equal(new DateOnly(2025, 5, 16), exp.UnadjustedThirdFriday);
        Assert.False(exp.IsQuarterly);
    }

    [Fact]
    public void GetMonthlyExpiration_ThirdFridayIsGoodFriday_ShiftsToThursday()
    {
        // April 2025: third Friday is 2025-04-18 (Good Friday). Expiration shifts to Thursday 04-17.
        var exp = Calendar().GetMonthlyExpiration(2025, 4);
        Assert.Equal(new DateOnly(2025, 4, 17), exp.Date);
        Assert.Equal(new DateOnly(2025, 4, 18), exp.UnadjustedThirdFriday);
    }

    [Fact]
    public void GetQuarterlyExpiration_March2025_IsQuadWitching()
    {
        var exp = Calendar().GetQuarterlyExpiration(2025, 1);
        Assert.Equal(new DateOnly(2025, 3, 21), exp.Date);
        Assert.True(exp.IsQuarterly);
        Assert.Equal(WitchingKind.QuadWitching, exp.Witching);
    }

    [Fact]
    public void GetWitchingDates_2025_ReturnsFourQuarterlies()
    {
        var dates = Calendar().GetWitchingDates(2025);
        Assert.Equal(4, dates.Count);
        Assert.All(dates, d => Assert.True(d.IsQuarterly));
    }

    [Fact]
    public void NextExpirationOnOrAfter_OnExpirationDay_ReturnsSameDay()
    {
        var exp = Calendar().NextExpirationOnOrAfter(new DateOnly(2025, 3, 21));
        Assert.Equal(new DateOnly(2025, 3, 21), exp.Date);
    }

    [Fact]
    public void NextExpirationOnOrAfter_QuarterlyOnly_SkipsMonthly()
    {
        var exp = Calendar().NextExpirationOnOrAfter(new DateOnly(2025, 5, 17), quarterlyOnly: true);
        Assert.Equal(new DateOnly(2025, 6, 20), exp.Date);
        Assert.True(exp.IsQuarterly);
    }

    // --- session window (DST) ---

    [Fact]
    public void GetSessionWindow_EdtTradingDay_OpensAt1330Utc()
    {
        var w = Calendar().GetSessionWindow(new DateOnly(2025, 5, 19))!;
        Assert.Equal(new DateTimeOffset(2025, 5, 19, 13, 30, 0, TimeSpan.Zero), w.RegularOpenUtc);
        Assert.Equal(new DateTimeOffset(2025, 5, 19, 20, 0, 0, TimeSpan.Zero), w.RegularCloseUtc);
    }

    [Fact]
    public void GetSessionWindow_AcrossSpringForward_OffsetChanges()
    {
        var cal = Calendar();
        // 2025-03-07 (Fri) is EST (UTC-5): 09:30 ET = 14:30 UTC.
        Assert.Equal(new DateTimeOffset(2025, 3, 7, 14, 30, 0, TimeSpan.Zero), cal.GetSessionWindow(new DateOnly(2025, 3, 7))!.RegularOpenUtc);
        // 2025-03-10 (Mon) is EDT (UTC-4): 09:30 ET = 13:30 UTC.
        Assert.Equal(new DateTimeOffset(2025, 3, 10, 13, 30, 0, TimeSpan.Zero), cal.GetSessionWindow(new DateOnly(2025, 3, 10))!.RegularOpenUtc);
    }

    [Fact]
    public void GetSessionWindow_HalfDay_ClosesAt1300EtAndOmitsPostMarket()
    {
        var w = Calendar().GetSessionWindow(new DateOnly(2025, 11, 28))!; // EST
        Assert.Equal(new DateTimeOffset(2025, 11, 28, 18, 0, 0, TimeSpan.Zero), w.RegularCloseUtc); // 13:00 EST = 18:00 UTC
        Assert.Null(w.PostMarketCloseUtc);
    }

    [Fact]
    public void GetSessionWindow_NonTradingDay_ReturnsNull()
    {
        Assert.Null(Calendar().GetSessionWindow(new DateOnly(2025, 12, 25)));
    }

    private sealed class TempConfig : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "marketdata-cfg-" + Guid.NewGuid().ToString("N"));

        public void WriteHolidays(int year, string json)
        {
            var dir = System.IO.Path.Combine(Path, "holidays");
            Directory.CreateDirectory(dir);
            File.WriteAllText(System.IO.Path.Combine(dir, $"holidays-{year}.json"), json);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true); }
            catch { /* best effort */ }
        }
    }
}
