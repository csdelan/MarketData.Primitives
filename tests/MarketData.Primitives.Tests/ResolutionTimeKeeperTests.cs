using System;
using Xunit;

namespace MarketData.Primitives.Tests
{
 [Collection("TimeKeeper")]
 public class ResolutionTimeKeeperTests
 {
 [Fact]
 public void GetNextEvent_Parameterless_UsesTimeKeeperNow()
 {
 var now = new DateTimeOffset(2023,2,15,0,0,0, TimeSpan.Zero);
 TimeKeeperProvider.SetSimulatedTimeKeeper(now);
 var res = new Resolution(1, ResolutionUnit.Quarters);
 var next = res.GetNextEvent();
 Assert.Equal(new DateTimeOffset(2023,4,1,0,0,0, TimeSpan.Zero), next);
 }

 [Fact]
 public void GetExactDuration_UsesTimeKeeperNow()
 {
 var now = new DateTimeOffset(2025,5,17,11,30,0, TimeSpan.Zero);
 TimeKeeperProvider.SetSimulatedTimeKeeper(now);
 var res = new Resolution(1, ResolutionUnit.Minutes);
 var duration = res.GetExactDuration();
 Assert.Equal(TimeSpan.FromMinutes(1), duration);
 }

 [Fact]
 public void GetLastEvent_FloorsToBoundary_ForMinutes()
 {
 var t = new DateTimeOffset(2025,5,17,11,34,45, TimeSpan.Zero); //11:34:45
 var res = new Resolution(5, ResolutionUnit.Minutes);
 var last = res.GetLastEvent(t);
 Assert.Equal(new DateTimeOffset(2025,5,17,11,30,0, TimeSpan.Zero), last);
 }

 [Fact]
 public void GetLastEvent_FloorsToBoundary_ForQuarters()
 {
 var t = new DateTimeOffset(2025,5,17,0,0,0, TimeSpan.Zero); // May17,2025
 var res = new Resolution(1, ResolutionUnit.Quarters);
 var last = res.GetLastEvent(t);
 Assert.Equal(new DateTimeOffset(2025,4,1,0,0,0, TimeSpan.Zero), last);
 }
 }
}
