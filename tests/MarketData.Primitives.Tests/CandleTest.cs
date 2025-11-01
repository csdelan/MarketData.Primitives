namespace MarketData.Primitives.Tests
{
    public class CandleTest
    {
        [Fact]
        public void DefaultConstructor_SetsEmptyResolution()
        {
            // Arrange & Act
            var candle = new Candle();

            // Assert
            Assert.Equal(Resolution.Empty, candle.Resolution);
            Assert.Equal(0m, candle.Open);
            Assert.Equal(0m, candle.High);
            Assert.Equal(0m, candle.Low);
            Assert.Equal(0m, candle.Close);
            Assert.Equal(0UL, candle.Volume);
            Assert.Equal(default(DateTimeOffset), candle.Timestamp);
        }

        [Fact]
        public void SingleValueConstructor_SetsAllPricesToValue()
        {
            // Arrange
            var value = 100m;
            var resolution = new Resolution(1, ResolutionUnit.Minutes);
            var timestamp = new DateTimeOffset(2025, 5, 16, 10, 0, 0, TimeSpan.Zero);

            // Act
            var candle = new Candle(value, resolution, timestamp);

            // Assert
            Assert.Equal(value, candle.Open);
            Assert.Equal(value, candle.High);
            Assert.Equal(value, candle.Low);
            Assert.Equal(value, candle.Close);
            Assert.Equal(0UL, candle.Volume);
            Assert.Equal(resolution, candle.Resolution);
            Assert.Equal(timestamp, candle.Timestamp);
        }

        [Fact]
        public void FullConstructor_ValidInputs_SetsPropertiesCorrectly()
        {
            // Arrange
            var open = 100m;
            var high = 110m;
            var low = 90m;
            var close = 105m;
            var volume = 1000UL;
            var resolution = new Resolution(1, ResolutionUnit.Minutes);
            var timestamp = new DateTimeOffset(2025, 5, 16, 10, 0, 0, TimeSpan.Zero);

            // Act
            var candle = new Candle(open, high, low, close, volume, resolution, timestamp);

            // Assert
            Assert.Equal(open, candle.Open);
            Assert.Equal(high, candle.High);
            Assert.Equal(low, candle.Low);
            Assert.Equal(close, candle.Close);
            Assert.Equal(volume, candle.Volume);
            Assert.Equal(resolution, candle.Resolution);
            Assert.Equal(timestamp, candle.Timestamp);
        }

        [Fact]
        public void FullConstructor_EmptyResolution_ThrowsArgumentException()
        {
            // Arrange
            var open = 100m;
            var high = 110m;
            var low = 90m;
            var close = 105m;
            var volume = 1000UL;
            var resolution = Resolution.Empty;
            var timestamp = new DateTimeOffset(2025, 5, 16, 10, 0, 0, TimeSpan.Zero);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Candle(open, high, low, close, volume, resolution, timestamp));
        }

        [Theory]
        [InlineData(100, 90, 80, 85, "Invalid price relationships.")] // high < low
        [InlineData(100, 110, 90, 120, "Invalid price relationships.")] // close > high
        [InlineData(100, 110, 90, 80, "Invalid price relationships.")] // close < low
        [InlineData(120, 110, 90, 100, "Invalid price relationships.")] // open > high
        [InlineData(80, 110, 90, 100, "Invalid price relationships.")] // open < low
        public void FullConstructor_InvalidPriceRelationships_ThrowsArgumentException(decimal open, decimal high, decimal low, decimal close, string expectedMessage)
        {
            // Arrange
            var volume = 1000UL;
            var resolution = new Resolution(1, ResolutionUnit.Minutes);
            var timestamp = new DateTimeOffset(2025, 5, 16, 10, 0, 0, TimeSpan.Zero);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Candle(open, high, low, close, volume, resolution, timestamp));
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TimestampMilliseconds_ReturnsCorrectUnixTime()
        {
            // Arrange
            var timestamp = new DateTimeOffset(2025, 5, 16, 10, 0, 0, TimeSpan.Zero);
            var candle = new Candle(100m, new Resolution(1, ResolutionUnit.Minutes), timestamp);
            var expectedMilliseconds = timestamp.ToUnixTimeMilliseconds();

            // Act
            var result = candle.TimestampMilliseconds;

            // Assert
            Assert.Equal(expectedMilliseconds, result);
        }

        [Fact]
        public void EndTime_WithValidResolution_ReturnsCorrectEndTime()
        {
            // Arrange
            var timestamp = new DateTimeOffset(2025, 5, 16, 11, 0, 0, TimeSpan.Zero);
            var resolution = new Resolution(1, ResolutionUnit.Hours);
            var candle = new Candle(100m, resolution, timestamp);
            var expectedEndTime = timestamp.AddHours(1);

            // Act
            var result = candle.EndTime;

            // Assert
            Assert.Equal(expectedEndTime, result);
        }

        [Fact]
        public void EndTime_WithEmptyResolution_ReturnsTimestamp()
        {
            // Arrange
            var timestamp = new DateTimeOffset(2025, 5, 16, 11, 0, 0, TimeSpan.Zero);
            var candle = new Candle(100m, Resolution.Empty, timestamp);

            // Act
            var end = candle.EndTime;

            // Assert
            Assert.Equal(timestamp, end);
        }

        [Fact]
        public void IsBullish_CloseGreaterThanOpen_ReturnsTrue()
        {
            // Arrange
            var candle = new Candle(100m, 110m, 90m, 105m, 1000UL, new Resolution(1, ResolutionUnit.Minutes));

            // Act
            var result = candle.IsBullish;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsBearish_CloseLessThanOpen_ReturnsTrue()
        {
            // Arrange
            var candle = new Candle(100m, 110m, 90m, 95m, 1000UL, new Resolution(1, ResolutionUnit.Minutes));

            // Act
            var result = candle.IsBearish;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDoji_BodyWithinThreshold_ReturnsTrue()
        {
            // Arrange
            var candle = new Candle(100m, 110m, 90m, 100.1m, 1000UL, new Resolution(1, ResolutionUnit.Minutes));
            var threshold = 0.05m;

            // Act
            var result = candle.IsDoji(threshold);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Score_BullishCandle_ReturnsScoreAbove50()
        {
            // Arrange
            var candle = new Candle(100m, 110m, 90m, 108m, 1000UL, new Resolution(1, ResolutionUnit.Minutes));

            // Act
            var score = candle.Score;

            // Assert
            Assert.True(score > 50);
            Assert.InRange(score, 0, 100);
        }

        [Fact]
        public void Score_BearishCandle_ReturnsScoreBelow50()
        {
            // Arrange
            var candle = new Candle(100m, 110m, 90m, 92m, 1000UL, new Resolution(1, ResolutionUnit.Minutes));

            // Act
            var score = candle.Score;

            // Assert
            Assert.True(score < 50);
            Assert.InRange(score, 0, 100);
        }

        [Fact]
        public void Score_DojiCandle_Returns50()
        {
            // Arrange
            var candle = new Candle(100m, 110m, 90m, 100m, 1000UL, new Resolution(1, ResolutionUnit.Minutes));

            // Act
            var score = candle.Score;

            // Assert
            Assert.Equal(50, score);
        }

        [Fact]
        public void Equality_SameValues_ReturnsTrue()
        {
            // Arrange
            var resolution = new Resolution(1, ResolutionUnit.Minutes);
            var timestamp = new DateTimeOffset(2025, 5, 16, 10, 0, 0, TimeSpan.Zero);
            var candle1 = new Candle(100m, 110m, 90m, 105m, 1000UL, resolution, timestamp);
            var candle2 = new Candle(100m, 110m, 90m, 105m, 1000UL, resolution, timestamp);

            // Act & Assert
            Assert.Equal(candle1, candle2);
            Assert.True(candle1.Equals(candle2));
            Assert.Equal(candle1.GetHashCode(), candle2.GetHashCode());
        }

        [Fact]
        public void Equality_DifferentValues_ReturnsFalse()
        {
            // Arrange
            var resolution = new Resolution(1, ResolutionUnit.Minutes);
            var timestamp = new DateTimeOffset(2025, 5, 16, 10, 0, 0, TimeSpan.Zero);
            var candle1 = new Candle(100m, 110m, 90m, 105m, 1000UL, resolution, timestamp);
            var candle2 = new Candle(100m, 110m, 90m, 106m, 1000UL, resolution, timestamp);

            // Act & Assert
            Assert.NotEqual(candle1, candle2);
            Assert.False(candle1.Equals(candle2));
        }
    }
}