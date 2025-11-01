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
 public void GetLastEvent_CurrentImplementation_ReturnsNow()
 {
 var anchor = new DateTimeOffset(2040,12,31,23,45,0, TimeSpan.Zero);
 TimeKeeperProvider.SetSimulatedTimeKeeper(anchor);
 var res = new Resolution(5, ResolutionUnit.Minutes);
 var last = res.GetLastEvent(anchor.AddHours(1));
 Assert.Equal(anchor, last);
 }
 }
}
