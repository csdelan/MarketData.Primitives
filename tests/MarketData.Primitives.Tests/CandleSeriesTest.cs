using MarketData.Primitives.Models;

namespace MarketData.Primitives.Tests
{
    public class CandleSeriesTest
    {
        private readonly Resolution _resolution = new Resolution(1, ResolutionUnit.Minutes);
        private readonly DateTimeOffset _baseTimestamp = new DateTimeOffset(2025, 5, 16, 10, 0, 0, TimeSpan.Zero);

        private Candle CreateCandle(decimal open, decimal high, decimal low, decimal close, ulong volume, DateTimeOffset timestamp)
        {
            return new Candle(open, high, low, close, volume, _resolution, timestamp);
        }

        [Fact]
        public void DefaultConstructor_InitializesEmptySeries()
        {
            // Arrange & Act
            var series = new CandleSeries();

            // Assert
            Assert.Empty(series.Candles);
            Assert.Throws<InvalidOperationException>(() => series.Open);
            Assert.Throws<InvalidOperationException>(() => series.High);
            Assert.Throws<InvalidOperationException>(() => series.Low);
            Assert.Throws<InvalidOperationException>(() => series.Close);
            Assert.Throws<InvalidOperationException>(() => series.Volume);
            Assert.Throws<InvalidOperationException>(() => series.Resolution);
        }

        [Fact]
        public void Constructor_WithCandles_AppendsCandlesCorrectly()
        {
            // Arrange
            var candles = new List<Candle>
            {
                CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp),
                CreateCandle(105m, 115m, 95m, 110m, 1200UL, _baseTimestamp.AddMinutes(1))
            };

            // Act
            var series = new CandleSeries(candles);

            // Assert
            Assert.Equal(2, series.Candles.Count);
            Assert.Equal(candles, series.Candles);
            Assert.Equal(100m, series.Open);
            Assert.Equal(115m, series.High);
            Assert.Equal(90m, series.Low);
            Assert.Equal(110m, series.Close);
            Assert.Equal(2200UL, series.Volume);
            Assert.Equal(_resolution, series.Resolution);
        }

        [Fact]
        public void AppendCandle_ValidCandle_AddsSuccessfully()
        {
            // Arrange
            var series = new CandleSeries();
            var candle = CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp);
            bool propertyChangedFired = false;
            series.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(series.Candles)) propertyChangedFired = true; };

            // Act
            series.AppendCandle(candle);

            // Assert
            Assert.Single(series.Candles);
            Assert.Equal(candle, series.Candles[0]);
            Assert.Equal(100m, series.Open);
            Assert.Equal(110m, series.High);
            Assert.Equal(90m, series.Low);
            Assert.Equal(105m, series.Close);
            Assert.Equal(1000UL, series.Volume);
            Assert.True(propertyChangedFired);
        }

        [Fact]
        public void AppendCandle_EmptyResolutionSingleValueCandle_ThrowsArgumentException()
        {
            // Arrange
            var series = new CandleSeries();
            var candle = new Candle(100m, Resolution.Empty, _baseTimestamp); // Single-value candle with empty resolution

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => series.AppendCandle(candle));
            Assert.Equal("Candle resolution cannot be empty.", exception.Message);
        }

        [Fact]
        public void AppendCandle_MismatchedResolution_ThrowsArgumentException()
        {
            // Arrange
            var series = new CandleSeries();
            var firstCandle = CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp);
            series.AppendCandle(firstCandle);
            var invalidCandle = new Candle(105m, 115m, 95m, 110m, 1200UL, new Resolution(1, ResolutionUnit.Hours), _baseTimestamp.AddMinutes(1));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => series.AppendCandle(invalidCandle));
        }

        [Fact]
        public void AppendCandle_NonIncreasingTimestamp_ThrowsArgumentException()
        {
            // Arrange
            var series = new CandleSeries();
            var firstCandle = CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp);
            series.AppendCandle(firstCandle);
            var invalidCandle = CreateCandle(105m, 115m, 95m, 110m, 1200UL, _baseTimestamp);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => series.AppendCandle(invalidCandle));
        }

        [Fact]
        public void AppendCandles_MultipleCandles_AddsSuccessfully()
        {
            // Arrange
            var series = new CandleSeries();
            var candles = new List<Candle>
            {
                CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp),
                CreateCandle(105m, 115m, 95m, 110m, 1200UL, _baseTimestamp.AddMinutes(1))
            };
            bool propertyChangedFired = false;
            series.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(series.Candles)) propertyChangedFired = true; };

            // Act
            series.AppendCandles(candles);

            // Assert
            Assert.Equal(2, series.Candles.Count);
            Assert.Equal(candles, series.Candles);
            Assert.Equal(115m, series.High);
            Assert.Equal(90m, series.Low);
            Assert.Equal(2200UL, series.Volume);
            Assert.True(propertyChangedFired);
        }

        [Fact]
        public void Consolidate_ValidSeries_ReturnsCompositeCandle()
        {
            // Arrange
            var series = new CandleSeries();
            var candles = new List<Candle>
            {
                CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp),
                CreateCandle(105m, 115m, 95m, 110m, 1200UL, _baseTimestamp.AddMinutes(1))
            };
            series.AppendCandles(candles);

            // Act
            var result = series.Consolidate();

            // Assert
            Assert.Equal(100m, result.Open);
            Assert.Equal(115m, result.High);
            Assert.Equal(90m, result.Low);
            Assert.Equal(110m, result.Close);
            Assert.Equal(2200UL, result.Volume);
            Assert.Equal(_baseTimestamp, result.Timestamp);
            Assert.Equal(new Resolution(2, ResolutionUnit.Minutes), result.Resolution);
        }

        [Fact]
        public void Consolidate_EmptySeries_ThrowsInvalidOperationException()
        {
            // Arrange
            var series = new CandleSeries();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => series.Consolidate());
        }

        [Fact]
        public void GetCandlestick_ExistingTimestamp_ReturnsCorrectCandle()
        {
            // Arrange
            var series = new CandleSeries();
            var candle = CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp);
            series.AppendCandle(candle);

            // Act
            var result = series.GetCandlestick(_baseTimestamp);

            // Assert
            Assert.Equal(candle, result);
        }

        [Fact]
        public void GetCandlestick_NonExistingTimestamp_ThrowsInvalidOperationException()
        {
            // Arrange
            var series = new CandleSeries();
            var candle = CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp);
            series.AppendCandle(candle);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => series.GetCandlestick(_baseTimestamp.AddMinutes(1)));
        }

        [Fact]
        public void GetGap_ValidCandle_ReturnsCorrectGap()
        {
            // Arrange
            var series = new CandleSeries();
            var firstCandle = CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp);
            var secondCandle = CreateCandle(108m, 115m, 95m, 110m, 1200UL, _baseTimestamp.AddMinutes(1));
            series.AppendCandles(new[] { firstCandle, secondCandle });

            // Act
            var gap = series.GetGap(secondCandle);

            // Assert
            Assert.Equal(3m, gap); // 108 - 105 = 3
        }

        [Fact]
        public void GetGap_FirstCandle_ReturnsZero()
        {
            // Arrange
            var series = new CandleSeries();
            var candle = CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp);
            series.AppendCandle(candle);

            // Act
            var gap = series.GetGap(candle);

            // Assert
            Assert.Equal(0m, gap);
        }

        [Fact]
        public void GetGap_NonExistingCandle_ReturnsZero()
        {
            // Arrange
            var series = new CandleSeries();
            var candle = CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp);
            var nonExistingCandle = CreateCandle(105m, 115m, 95m, 110m, 1200UL, _baseTimestamp.AddMinutes(1));

            // Act
            var gap = series.GetGap(nonExistingCandle);

            // Assert
            Assert.Equal(0m, gap);
        }

        [Fact]
        public void CopyRange_ValidIndices_ReturnsCorrectSeries()
        {
            // Arrange
            var series = new CandleSeries();
            var candles = new List<Candle>
    {
        CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp),
        CreateCandle(105m, 115m, 95m, 110m, 1200UL, _baseTimestamp.AddMinutes(1)),
        CreateCandle(110m, 120m, 100m, 115m, 1500UL, _baseTimestamp.AddMinutes(2))
    };
            series.AppendCandles(candles);

            // Act
            var result = series.CopyRange(1, 2);

            // Assert
            Assert.Equal(2, result.Candles.Count);
            Assert.Equal(candles[1], result.Candles[0]);
            Assert.Equal(candles[2], result.Candles[1]);
            Assert.Equal(120m, result.High);
            Assert.Equal(95m, result.Low);
            Assert.Equal(2700UL, result.Volume);
        }

        [Theory]
        [InlineData(-1, 1, "Invalid index range.")]
        [InlineData(2, 1, "Invalid index range.")]
        [InlineData(0, 3, "Invalid index range.")]
        public void CopyRange_InvalidIndices_ThrowsArgumentException(int startIndex, int endIndex, string expectedMessage)
        {
            // Arrange
            var series = new CandleSeries();
            var candles = new List<Candle>
            {
                CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp),
                CreateCandle(105m, 115m, 95m, 110m, 1200UL, _baseTimestamp.AddMinutes(1))
            };
            series.AppendCandles(candles);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => series.CopyRange(startIndex, endIndex));
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void Enumerator_IteratesCorrectly()
        {
            // Arrange
            var series = new CandleSeries();
            var candles = new List<Candle>
            {
                CreateCandle(100m, 110m, 90m, 105m, 1000UL, _baseTimestamp),
                CreateCandle(105m, 115m, 95m, 110m, 1200UL, _baseTimestamp.AddMinutes(1))
            };
            series.AppendCandles(candles);

            // Act
            var enumerated = series.ToList();

            // Assert
            Assert.Equal(candles, enumerated);
        }
    }
}