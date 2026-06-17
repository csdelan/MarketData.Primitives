using Core;
using MarketData.Application.Calendar;
using MarketData.Application.Services;

namespace MarketData.Primitives.Tests;

public class MarketClockTests
{
    // 2025-05-19 is a Monday NYSE trading day (EDT, UTC-4):
    // 09:30 ET = 13:30 UTC (open), 16:00 ET = 20:00 UTC (close).

    private static MarketClock ClockAt(DateTimeOffset utc)
    {
        var time = new ManualTimeProvider(utc);
        var calendar = new NyseMarketCalendar(overrides: new HolidayOverrideLoader(
            Path.Combine(Path.GetTempPath(), "marketdata-tests-" + Guid.NewGuid().ToString("N"))));
        var provider = new MarketContextProvider(calendar, time);
        return new MarketClock(provider, time);
    }

    [Fact]
    public void IsMarketOpen_WithinSession_UsesInjectedClock()
    {
        var clock = ClockAt(new DateTimeOffset(2025, 5, 19, 15, 0, 0, TimeSpan.Zero)); // 11:00 ET
        Assert.True(clock.IsMarketOpen());
    }

    [Fact]
    public void IsMarketOpen_AfterClose_ReturnsFalse()
    {
        var clock = ClockAt(new DateTimeOffset(2025, 5, 19, 20, 30, 0, TimeSpan.Zero)); // 16:30 ET
        Assert.False(clock.IsMarketOpen());
    }

    [Fact]
    public void TodayCloseUtc_IsFourPmEasternInUtc()
    {
        var clock = ClockAt(new DateTimeOffset(2025, 5, 19, 14, 0, 0, TimeSpan.Zero)); // mid-session
        Assert.Equal(new DateTimeOffset(2025, 5, 19, 20, 0, 0, TimeSpan.Zero), clock.TodayCloseUtc());
    }

    [Fact]
    public void TodayCloseUtc_OnHoliday_ReturnsNull()
    {
        var clock = ClockAt(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero)); // July 4 holiday
        Assert.Null(clock.TodayCloseUtc());
    }

    [Fact]
    public void NextMarketOpen_FromFridayEvening_ReturnsMonday()
    {
        // 2025-05-17 01:00 UTC = Friday 2025-05-16 21:00 ET.
        var clock = ClockAt(new DateTimeOffset(2025, 5, 17, 1, 0, 0, TimeSpan.Zero));
        Assert.Equal(new DateTimeOffset(2025, 5, 19, 13, 30, 0, TimeSpan.Zero), clock.NextMarketOpen());
    }

    [Fact]
    public void TradingDaysUntil_FutureDate_IsExclusiveOfToday()
    {
        var clock = ClockAt(new DateTimeOffset(2025, 5, 19, 15, 0, 0, TimeSpan.Zero)); // today = Mon 05-19
        Assert.Equal(4, clock.TradingDaysUntil(new DateOnly(2025, 5, 23))); // Tue..Fri
    }

    [Fact]
    public void Context_ExposesLiquidityAndDay()
    {
        var clock = ClockAt(new DateTimeOffset(2025, 5, 19, 15, 0, 0, TimeSpan.Zero));
        var ctx = clock.Context();
        Assert.Equal("US-EQ", ctx.VenueId);
        Assert.True(ctx.IsRegularSessionOpen);
        Assert.True(ctx.Day.IsTradingDay);
    }
}
