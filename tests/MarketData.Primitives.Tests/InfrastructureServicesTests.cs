using MarketData.Application.Contracts;
using MarketData.Infrastructure.Calendar;
using MarketData.Infrastructure.TimeKeeping;

namespace MarketData.Primitives.Tests;

public class InfrastructureServicesTests
{
    [Fact]
    public async Task NyseCalendar_ReturnsFalse_OnWeekend()
    {
        var sut = new NyseMarketCalendarService();

        var result = await sut.IsTradingDayAsync("NYSE", new DateOnly(2025, 5, 17)); // Saturday

        Assert.False(result);
    }

    [Fact]
    public async Task NyseCalendar_ReturnsFalse_OnObservedHoliday()
    {
        var sut = new NyseMarketCalendarService();

        var result = await sut.IsTradingDayAsync("NYSE", new DateOnly(2027, 12, 24)); // Christmas observed

        Assert.False(result);
    }

    [Fact]
    public async Task NyseHours_ReturnsOpen_WhenWithinSessionOnTradingDay()
    {
        var timeKeeper = new FakeTimeKeeper(new DateTimeOffset(2025, 5, 19, 15, 0, 0, TimeSpan.Zero)); // 11:00 ET
        var calendar = new NyseMarketCalendarService();
        var sut = new NyseMarketHoursService(timeKeeper, calendar);

        var isOpen = await sut.IsOpenAsync("NYSE", timeKeeper.Now);

        Assert.True(isOpen);
    }

    [Fact]
    public async Task NyseHours_CurrentStatus_UsesTimeKeeperNow()
    {
        var now = new DateTimeOffset(2025, 5, 19, 20, 30, 0, TimeSpan.Zero); // 16:30 ET, after close
        var timeKeeper = new FakeTimeKeeper(now);
        var calendar = new NyseMarketCalendarService();
        var sut = new NyseMarketHoursService(timeKeeper, calendar);

        var status = await sut.GetCurrentStatusAsync("NYSE");

        Assert.True(status.IsTradingDay);
        Assert.False(status.IsOpen);
        Assert.NotNull(status.Session);
    }

    [Fact]
    public async Task RealTimeTimeKeeper_SetTime_Throws()
    {
        var sut = new RealTimeTimeKeeper();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetTime(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task RealTimeTimeKeeper_WaitTime_PastTime_CompletesImmediately()
    {
        var sut = new RealTimeTimeKeeper();

        await sut.WaitTime(DateTimeOffset.UtcNow.AddMilliseconds(-1));
    }

    private sealed class FakeTimeKeeper(DateTimeOffset now) : ITimeKeeper
    {
        public DateTimeOffset Now { get; private set; } = now;

        public Task SetTime(DateTimeOffset time)
        {
            Now = time;
            return Task.CompletedTask;
        }

        public Task WaitTime(DateTimeOffset time)
        {
            Now = time;
            return Task.CompletedTask;
        }
    }
}
