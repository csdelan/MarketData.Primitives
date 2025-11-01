using System;
using Xunit;

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
 System.Threading.Thread.Sleep(5);
 var t2 = TimeKeeperProvider.Now;
 Assert.True(t2 >= t1);
 }

 [Fact]
 public void SimulatedTimeKeeper_SetAndWaitTime_Works()
 {
 var start = new DateTimeOffset(2025,1,1,0,0,0, TimeSpan.Zero);
 TimeKeeperProvider.SetSimulatedTimeKeeper(start);
 Assert.Equal(start, TimeKeeperProvider.Now);

 var next = start.AddMinutes(5);
 // SetTime moves immediately
 TimeKeeperProvider.Current.SetTime(next).GetAwaiter().GetResult();
 Assert.Equal(next, TimeKeeperProvider.Now);

 // WaitTime with earlier time is no-op
 TimeKeeperProvider.Current.WaitTime(start).GetAwaiter().GetResult();
 Assert.Equal(next, TimeKeeperProvider.Now);

 // WaitTime with later time moves simulated clock
 var later = next.AddMinutes(10);
 TimeKeeperProvider.Current.WaitTime(later).GetAwaiter().GetResult();
 Assert.Equal(later, TimeKeeperProvider.Now);
 }

 [Fact]
 public void Reset_ToRealTimeKeeper_Works()
 {
 var start = new DateTimeOffset(2030,1,1,0,0,0, TimeSpan.Zero);
 TimeKeeperProvider.SetSimulatedTimeKeeper(start);
 Assert.Equal(start, TimeKeeperProvider.Now);

 TimeKeeperProvider.SetRealTimeKeeper();
 var now = TimeKeeperProvider.Now;
 Assert.NotEqual(start, now);
 }
 }
}
