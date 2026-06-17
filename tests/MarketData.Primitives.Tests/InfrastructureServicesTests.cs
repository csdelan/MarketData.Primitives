using Core;
using MarketData.Application.Calendar;

namespace MarketData.Primitives.Tests;

public class InfrastructureServicesTests
{
    // 2025-05-19 is a Monday and a regular NYSE trading day (EDT, UTC-4):
    // 09:30 ET = 13:30 UTC (open), 16:00 ET = 20:00 UTC (close).

    [Fact]
    public async Task NyseHours_ReturnsOpen_WhenWithinSessionOnTradingDay()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2025, 5, 19, 15, 0, 0, TimeSpan.Zero)); // 11:00 ET
        var sut = new NyseMarketHoursService(clock);

        var isOpen = await sut.IsOpenAsync("NYSE", clock.GetUtcNow());

        Assert.True(isOpen);
    }

    [Fact]
    public async Task NyseHours_CurrentStatus_UsesInjectedClock()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2025, 5, 19, 20, 30, 0, TimeSpan.Zero)); // 16:30 ET, after close
        var sut = new NyseMarketHoursService(clock);

        var status = await sut.GetCurrentStatusAsync("NYSE");

        Assert.True(status.IsTradingDay);
        Assert.False(status.IsOpen);
        Assert.NotNull(status.Session);
    }

    [Fact]
    public async Task NyseHours_TodayCloseUtc_IsFourPmEasternInUtc()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2025, 5, 19, 14, 0, 0, TimeSpan.Zero)); // mid-session
        var sut = new NyseMarketHoursService(clock);

        var closeUtc = await sut.GetTodayCloseUtcAsync("NYSE");

        Assert.Equal(new DateTimeOffset(2025, 5, 19, 20, 0, 0, TimeSpan.Zero), closeUtc); // 16:00 ET = 20:00 UTC (EDT)
    }
}
