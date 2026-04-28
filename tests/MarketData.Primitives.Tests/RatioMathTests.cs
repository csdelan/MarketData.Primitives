namespace MarketData.Primitives.Tests
{
    public class RatioMathTests
    {
        private static readonly Resolution OneMinute = new(1, ResolutionUnit.Minutes);
        private static readonly Resolution FiveMinutes = new(5, ResolutionUnit.Minutes);

        private static DateTimeOffset Ts(int minute) =>
            new(2025, 5, 16, 10, minute, 0, TimeSpan.Zero);

        [Fact]
        public void CombineCandles_DivisionMath_OHLCComputedCorrectly()
        {
            var num = new Candle(open: 400m, high: 420m, low: 390m, close: 410m, volume: 1000UL,
                resolution: OneMinute, timestamp: Ts(0));
            var den = new Candle(open: 200m, high: 210m, low: 195m, close: 205m, volume: 500UL,
                resolution: OneMinute, timestamp: Ts(0));

            var ratio = RatioMath.CombineCandles(num, den);

            Assert.Equal(400m / 200m, ratio.Open);
            Assert.Equal(410m / 205m, ratio.Close);
            // High = numHigh / denLow ; Low = numLow / denHigh — preserves ordering invariants
            Assert.Equal(420m / 195m, ratio.High);
            Assert.Equal(390m / 210m, ratio.Low);
            Assert.Equal(0UL, ratio.Volume);
            Assert.Equal(OneMinute, ratio.Resolution);
            Assert.Equal(Ts(0), ratio.Timestamp);

            Assert.True(ratio.High >= ratio.Open);
            Assert.True(ratio.High >= ratio.Close);
            Assert.True(ratio.Low <= ratio.Open);
            Assert.True(ratio.Low <= ratio.Close);
        }

        [Fact]
        public void CombineCandles_ZeroDenominatorOpen_Throws()
        {
            var num = new Candle(400m, 420m, 390m, 410m, 1000UL, OneMinute, Ts(0));
            var den = new Candle(0m, 210m, 195m, 205m, 500UL, OneMinute, Ts(0));

            Assert.Throws<DivideByZeroException>(() => RatioMath.CombineCandles(num, den));
        }

        [Fact]
        public void CombineCandles_ZeroDenominatorLow_Throws()
        {
            var num = new Candle(400m, 420m, 390m, 410m, 1000UL, OneMinute, Ts(0));
            var den = new Candle(200m, 210m, 0m, 205m, 500UL, OneMinute, Ts(0));

            Assert.Throws<DivideByZeroException>(() => RatioMath.CombineCandles(num, den));
        }

        [Fact]
        public void CombineCandles_TimestampMismatch_Throws()
        {
            var num = new Candle(400m, 420m, 390m, 410m, 1000UL, OneMinute, Ts(0));
            var den = new Candle(200m, 210m, 195m, 205m, 500UL, OneMinute, Ts(1));

            Assert.Throws<ArgumentException>(() => RatioMath.CombineCandles(num, den));
        }

        [Fact]
        public void CombineCandles_ResolutionMismatch_Throws()
        {
            var num = new Candle(400m, 420m, 390m, 410m, 1000UL, OneMinute, Ts(0));
            var den = new Candle(200m, 210m, 195m, 205m, 500UL, FiveMinutes, Ts(0));

            Assert.Throws<ArgumentException>(() => RatioMath.CombineCandles(num, den));
        }

        [Fact]
        public void CombineCandles_NullArguments_Throws()
        {
            var c = new Candle(400m, 420m, 390m, 410m, 1000UL, OneMinute, Ts(0));

            Assert.Throws<ArgumentNullException>(() => RatioMath.CombineCandles(null!, c));
            Assert.Throws<ArgumentNullException>(() => RatioMath.CombineCandles(c, null!));
        }

        [Fact]
        public void CombineSeries_InnerJoinsOnTimestamp_DropsUnpairedBars()
        {
            var num = new List<Candle>
            {
                new(400m, 420m, 390m, 410m, 1000UL, OneMinute, Ts(0)),
                new(401m, 421m, 391m, 411m, 1000UL, OneMinute, Ts(1)),
                new(402m, 422m, 392m, 412m, 1000UL, OneMinute, Ts(2)),
            };
            var den = new List<Candle>
            {
                new(200m, 210m, 195m, 205m, 500UL, OneMinute, Ts(0)),
                // gap at Ts(1)
                new(202m, 212m, 197m, 207m, 500UL, OneMinute, Ts(2)),
                new(203m, 213m, 198m, 208m, 500UL, OneMinute, Ts(3)),
            };

            var result = RatioMath.CombineSeries(num, den);

            Assert.Equal(2, result.Count);
            Assert.Equal(Ts(0), result[0].Timestamp);
            Assert.Equal(Ts(2), result[1].Timestamp);
        }

        [Fact]
        public void CombineSeries_OutputSortedAscending()
        {
            var num = new List<Candle>
            {
                new(402m, 422m, 392m, 412m, 1000UL, OneMinute, Ts(2)),
                new(400m, 420m, 390m, 410m, 1000UL, OneMinute, Ts(0)),
                new(401m, 421m, 391m, 411m, 1000UL, OneMinute, Ts(1)),
            };
            var den = new List<Candle>
            {
                new(201m, 211m, 196m, 206m, 500UL, OneMinute, Ts(1)),
                new(202m, 212m, 197m, 207m, 500UL, OneMinute, Ts(2)),
                new(200m, 210m, 195m, 205m, 500UL, OneMinute, Ts(0)),
            };

            var result = RatioMath.CombineSeries(num, den);

            Assert.Equal(3, result.Count);
            Assert.Equal(Ts(0), result[0].Timestamp);
            Assert.Equal(Ts(1), result[1].Timestamp);
            Assert.Equal(Ts(2), result[2].Timestamp);
        }

        [Fact]
        public void CombineSeries_DropsBarsWithZeroDenominator()
        {
            var num = new List<Candle>
            {
                new(400m, 420m, 390m, 410m, 1000UL, OneMinute, Ts(0)),
                new(401m, 421m, 391m, 411m, 1000UL, OneMinute, Ts(1)),
            };
            var den = new List<Candle>
            {
                new(200m, 210m, 195m, 205m, 500UL, OneMinute, Ts(0)),
                new(0m, 211m, 196m, 206m, 500UL, OneMinute, Ts(1)),
            };

            var result = RatioMath.CombineSeries(num, den);

            Assert.Single(result);
            Assert.Equal(Ts(0), result[0].Timestamp);
        }

        [Fact]
        public void CombineSeries_EmptyInputs_ReturnsEmpty()
        {
            var result = RatioMath.CombineSeries(new List<Candle>(), new List<Candle>());

            Assert.Empty(result);
        }

        [Fact]
        public void CombineSeries_NullArguments_Throws()
        {
            var list = new List<Candle>();

            Assert.Throws<ArgumentNullException>(() => RatioMath.CombineSeries(null!, list));
            Assert.Throws<ArgumentNullException>(() => RatioMath.CombineSeries(list, null!));
        }
    }
}
