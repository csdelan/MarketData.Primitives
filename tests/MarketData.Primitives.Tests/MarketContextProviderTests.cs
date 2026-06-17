using MarketData.Application.Calendar;
using MarketData.Application.Contracts;
using MarketData.Primitives.Sessions;

namespace MarketData.Primitives.Tests;

public class MarketContextProviderTests
{
    private static readonly TimeSpan Edt = TimeSpan.FromHours(-4);
    private static readonly TimeSpan Est = TimeSpan.FromHours(-5);

    private static MarketContextProvider Provider() =>
        new(new NyseMarketCalendar(overrides: new HolidayOverrideLoader(
                Path.Combine(Path.GetTempPath(), "marketdata-tests-" + Guid.NewGuid().ToString("N")))),
            TimeProvider.System);

    // --- phase / liquidity ---

    [Fact]
    public void GetActivePhase_At0935Edt_ReturnsRegular()
    {
        var instant = new DateTimeOffset(2025, 5, 19, 9, 35, 0, Edt);
        Assert.Equal(MarketPhase.Regular, Provider().GetActivePhase(instant));
        Assert.Equal(SessionLiquidityLevel.Full, Provider().GetLiquidity(instant));
    }

    [Fact]
    public void GetActivePhase_At0400Edt_ReturnsPreMarket()
    {
        var instant = new DateTimeOffset(2025, 5, 19, 4, 0, 0, Edt);
        Assert.Equal(MarketPhase.PreMarket, Provider().GetActivePhase(instant));
        Assert.Equal(SessionLiquidityLevel.Reduced, Provider().GetLiquidity(instant));
    }

    [Fact]
    public void GetActivePhase_At1600EdtSharp_ReturnsPostMarket()
    {
        var instant = new DateTimeOffset(2025, 5, 19, 16, 0, 0, Edt);
        Assert.Equal(MarketPhase.PostMarket, Provider().GetActivePhase(instant));
    }

    [Fact]
    public void GetActivePhase_Saturday_ReturnsClosed()
    {
        var instant = new DateTimeOffset(2025, 5, 17, 12, 0, 0, Edt);
        Assert.Equal(MarketPhase.Closed, Provider().GetActivePhase(instant));
        Assert.Equal(SessionLiquidityLevel.None, Provider().GetLiquidity(instant));
    }

    [Fact]
    public void GetActivePhase_HalfDayAfterClose_FuturesGiveLightLiquidity()
    {
        // 2025-11-28 (half day, EST). 14:00 ET is after the 13:00 close; equities post-market is
        // omitted, but the overnight futures session is still trading → Light.
        var instant = new DateTimeOffset(2025, 11, 28, 14, 0, 0, Est);
        Assert.Equal(SessionLiquidityLevel.Light, Provider().GetLiquidity(instant));
    }

    [Fact]
    public void GetLiquidity_OvernightWeeknight_ReturnsLight()
    {
        var instant = new DateTimeOffset(2025, 5, 20, 22, 0, 0, Edt); // Tue 22:00 ET
        Assert.Equal(SessionLiquidityLevel.Light, Provider().GetLiquidity(instant));
    }

    [Fact]
    public void GetLiquidity_FridayLateEvening_ReturnsNone()
    {
        var instant = new DateTimeOffset(2025, 5, 16, 20, 30, 0, Edt); // Fri 20:30 ET, weekend gap
        Assert.Equal(SessionLiquidityLevel.None, Provider().GetLiquidity(instant));
    }

    [Fact]
    public void GetLiquidity_SundayBeforeAndAfterOvernightOpen()
    {
        Assert.Equal(SessionLiquidityLevel.None, Provider().GetLiquidity(new DateTimeOffset(2025, 5, 18, 17, 0, 0, Edt)));
        Assert.Equal(SessionLiquidityLevel.Light, Provider().GetLiquidity(new DateTimeOffset(2025, 5, 18, 18, 30, 0, Edt)));
    }

    // --- context math ---

    [Fact]
    public void GetContextAt_MidSession_RegularProgressIsHalf()
    {
        var instant = new DateTimeOffset(2025, 5, 19, 12, 45, 0, Edt); // 195 of 390 minutes
        var ctx = Provider().GetContextAt(instant);
        Assert.True(ctx.IsRegularSessionOpen);
        Assert.NotNull(ctx.RegularProgress);
        Assert.Equal(0.5, ctx.RegularProgress!.Value, 3);
    }

    [Fact]
    public void GetContextAt_AfterClose_NextOpenIsNextTradingDay()
    {
        var instant = new DateTimeOffset(2025, 5, 19, 16, 30, 0, Edt);
        var ctx = Provider().GetContextAt(instant);
        Assert.False(ctx.IsRegularSessionOpen);
        Assert.Equal(new DateTimeOffset(2025, 5, 20, 13, 30, 0, TimeSpan.Zero), ctx.NextRegularOpenUtc);
    }

    [Fact]
    public void GetContextAt_FridayAfterClose_NextOpenSkipsWeekend()
    {
        var instant = new DateTimeOffset(2025, 5, 16, 17, 0, 0, Edt);
        var ctx = Provider().GetContextAt(instant);
        Assert.Equal(new DateTimeOffset(2025, 5, 19, 13, 30, 0, TimeSpan.Zero), ctx.NextRegularOpenUtc);
    }

    [Fact]
    public void GetContextAt_DayBeforeHoliday_NextOpenSkipsHoliday()
    {
        var instant = new DateTimeOffset(2025, 7, 3, 16, 30, 0, Edt); // Thu before July 4 holiday
        var ctx = Provider().GetContextAt(instant);
        Assert.Equal(new DateTimeOffset(2025, 7, 7, 13, 30, 0, TimeSpan.Zero), ctx.NextRegularOpenUtc);
    }

    [Fact]
    public void GetContextAt_SundayOvernight_TradingDateIsNextMonday()
    {
        var instant = new DateTimeOffset(2025, 5, 18, 18, 30, 0, Edt);
        var ctx = Provider().GetContextAt(instant);
        Assert.Equal(MarketPhase.OvernightFutures, ctx.ActivePhase);
        Assert.Equal(new DateOnly(2025, 5, 19), ctx.TradingDate);
    }

    [Fact]
    public void GetContextAt_LastTradingDayOfMonth_FlagsMonthAndWeekEnd()
    {
        // 2025-05-30 (Fri) is the last trading day of May and of its week.
        var instant = new DateTimeOffset(2025, 5, 30, 11, 0, 0, Edt);
        var ctx = Provider().GetContextAt(instant);
        Assert.True(ctx.IsMonthEnd);
        Assert.True(ctx.IsWeekEnd);
        Assert.False(ctx.IsQuarterEnd);
    }

    // --- extensibility ---

    [Fact]
    public void GetActivePhase_AlternateSchedule_FollowsCustomHours()
    {
        var schedule = new VenueSchedule(
            venueId: "TEST",
            timeZoneId: "America/New_York",
            regularOpen: new TimeOnly(10, 0),
            regularClose: new TimeOnly(15, 0),
            halfDayClose: new TimeOnly(12, 0),
            preMarketOpen: new TimeOnly(8, 0),
            postMarketClose: new TimeOnly(17, 0),
            hasOvernightFutures: false);
        var provider = new MarketContextProvider(
            new NyseMarketCalendar(schedule, new HolidayOverrideLoader(
                Path.Combine(Path.GetTempPath(), "marketdata-tests-" + Guid.NewGuid().ToString("N")))),
            TimeProvider.System);

        Assert.Equal(MarketPhase.Regular, provider.GetActivePhase(new DateTimeOffset(2025, 5, 19, 10, 30, 0, Edt)));
        Assert.Equal(MarketPhase.PreMarket, provider.GetActivePhase(new DateTimeOffset(2025, 5, 19, 9, 0, 0, Edt)));
        Assert.Equal(MarketPhase.PostMarket, provider.GetActivePhase(new DateTimeOffset(2025, 5, 19, 16, 0, 0, Edt)));
        // No overnight for this venue.
        Assert.Equal(MarketPhase.Closed, provider.GetActivePhase(new DateTimeOffset(2025, 5, 19, 18, 30, 0, Edt)));
    }
}
