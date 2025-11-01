namespace MarketData.Primitives.Tests
{
    [Collection("TimeKeeper")]
    public class TimeKeeperProviderTest
    {
        [Fact]
        public void Now_RealTimeKeeper_AdvancesOverTime()
        {
            TimeKeeperProvider.SetRealTimeKeeper();
            var t1 = TimeKeeperProvider.Now;
            // small delay
            Thread.Sleep(5);
            var t2 = TimeKeeperProvider.Now;
            Assert.True(t2 >= t1);
        }

        [Fact]
        public async Task SimulatedTimeKeeper_SetAndWaitTime_Works()
        {
            var start = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
            TimeKeeperProvider.SetSimulatedTimeKeeper(start);
            Assert.Equal(start, TimeKeeperProvider.Now);

            var next = start.AddMinutes(5);
            // SetTime moves immediately
            await TimeKeeperProvider.Current.SetTime(next);
            Assert.Equal(next, TimeKeeperProvider.Now);

            // WaitTime with earlier time is no-op
            await TimeKeeperProvider.Current.WaitTime(start);
            Assert.Equal(next, TimeKeeperProvider.Now);

            // WaitTime with later time moves simulated clock
            var later = next.AddMinutes(10);
            await TimeKeeperProvider.Current.WaitTime(later);
            Assert.Equal(later, TimeKeeperProvider.Now);
        }

        [Fact]
        public void Reset_ToRealTimeKeeper_Works()
        {
            var start = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
            TimeKeeperProvider.SetSimulatedTimeKeeper(start);
            Assert.Equal(start, TimeKeeperProvider.Now);

            TimeKeeperProvider.SetRealTimeKeeper();
            var now = TimeKeeperProvider.Now;
            Assert.NotEqual(start, now);
        }
    }
}
