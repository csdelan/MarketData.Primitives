namespace MarketData.Primitives.Tests
{
    public class ResolutionTest
    {
        [Fact]
        public void IntradayTest()
        {
            var res = new Resolution(1, ResolutionUnit.Days);
            Assert.False(res.IsIntraday);
            res = new Resolution(6, ResolutionUnit.Hours);
            Assert.True(res.IsIntraday);
            res = new Resolution(23, ResolutionUnit.Hours);
            Assert.True(res.IsIntraday);
            res = new Resolution(24, ResolutionUnit.Hours);
            Assert.False(res.IsIntraday);
        }

        [Theory]
        [InlineData(0, ResolutionUnit.Seconds, 0)]
        [InlineData(1, ResolutionUnit.Seconds, 1)]
        [InlineData(10, ResolutionUnit.Seconds, 10)]
        [InlineData(1, ResolutionUnit.Minutes, 60)]
        [InlineData(2, ResolutionUnit.Minutes, 120)]
        [InlineData(1, ResolutionUnit.Hours, 3600)]
        [InlineData(2, ResolutionUnit.Hours, 7200)]
        [InlineData(1, ResolutionUnit.Days, 86400)]
        [InlineData(2, ResolutionUnit.Days, 172800)]
        [InlineData(1, ResolutionUnit.Weeks, 604800)]
        [InlineData(2, ResolutionUnit.Weeks, 1209600)]
        public void GetTimeSpan_FixedUnits_ReturnsExpectedSeconds(uint count, ResolutionUnit unit, int expectedSeconds)
        {
            var resolution = new Resolution(count, unit);
            var ts = resolution.GetTimeSpan();
            Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), ts);
        }

        [Theory]
        [InlineData(1, ResolutionUnit.Months)]
        [InlineData(1, ResolutionUnit.Quarters)]
        [InlineData(1, ResolutionUnit.Years)]
        public void GetTimeSpan_VariableLengthUnits_Throws(uint count, ResolutionUnit unit)
        {
            var resolution = new Resolution(count, unit);
            Assert.Throws<InvalidOperationException>(() => resolution.GetTimeSpan());
        }

        [Fact]
        public void GetTimeSpan_ZeroCount_ReturnsZero()
        {
            var resolution = new Resolution(0, ResolutionUnit.Minutes);
            Assert.Equal(TimeSpan.Zero, resolution.GetTimeSpan());
        }

        [Fact]
        public void ShorthandTest()
        {
            var res = new Resolution(1, ResolutionUnit.Days);
            Assert.Equal("1d", res.ToShorthand());
            res = new Resolution(2, ResolutionUnit.Weeks);
            Assert.Equal("2w", res.ToShorthand());
            res = new Resolution(2, ResolutionUnit.Months);
            Assert.Equal("2M", res.ToShorthand());
            res = new Resolution(4, ResolutionUnit.Minutes);
            Assert.Equal("4m", res.ToShorthand());
            res = new Resolution(6, ResolutionUnit.Hours);
            Assert.Equal("6h", res.ToShorthand());
            res = new Resolution(12, ResolutionUnit.Seconds);
            Assert.Equal("12s", res.ToShorthand());
        }

        public static IEnumerable<object[]> GetNextQuarterTestData()
        {
            yield return new object[] { new DateTimeOffset(2023, 2, 15, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 4, 1, 0, 0, 0, TimeSpan.Zero) };
            yield return new object[] { new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 4, 1, 0, 0, 0, TimeSpan.Zero) };
            yield return new object[] { new DateTimeOffset(2023, 5, 15, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 7, 1, 0, 0, 0, TimeSpan.Zero) };
            yield return new object[] { new DateTimeOffset(2023, 6, 30, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 7, 1, 0, 0, 0, TimeSpan.Zero) };
            yield return new object[] { new DateTimeOffset(2023, 7, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero) };
            yield return new object[] { new DateTimeOffset(2023, 8, 15, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero) };
            yield return new object[] { new DateTimeOffset(2023, 11, 15, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
            yield return new object[] { new DateTimeOffset(2023, 12, 31, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        }

        [Theory]
        [MemberData(nameof(GetNextQuarterTestData))]
        public void GetNextQuarterTest(DateTimeOffset input, DateTimeOffset expected)
        {
            var res = new Resolution(1, ResolutionUnit.Quarters);
            var nextQuarter = res.GetNextEvent(input);
            Assert.Equal(expected, nextQuarter);
        }

        [Fact]
        public void GetExactDuration_1Month_February2025_Returns28Days()
        {
            var resolution = new Resolution(1, ResolutionUnit.Months);
            var start = new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero);
            var duration = resolution.GetDurationToNextResolutionEvent(start);
            Assert.Equal(TimeSpan.FromDays(28), duration);
        }

        [Fact]
        public void GetExactDuration_1Quarter_Q12025_Returns90Days()
        {
            var resolution = new Resolution(1, ResolutionUnit.Quarters);
            var start = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var duration = resolution.GetDurationToNextResolutionEvent(start);
            Assert.Equal(TimeSpan.FromDays(90), duration); // Jan 1 to Apr 1, 2025
        }

        [Fact]
        public void GetExactDuration_1Minute_Returns60Seconds()
        {
            var resolution = new Resolution(1, ResolutionUnit.Minutes);
            var start = new DateTimeOffset(2025, 5, 17, 11, 30, 0, TimeSpan.Zero);
            var duration = resolution.GetDurationToNextResolutionEvent(start);
            Assert.Equal(TimeSpan.FromMinutes(1), duration);
        }
    }
}