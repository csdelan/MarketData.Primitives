namespace MarketData.Primitives.Tests
{
    public class BarTest
    {

        [Fact]
        public void Bar_ValidOHLC_ComputesPropertiesCorrectly()
        {
            // Arrange
            var bar = new Bar { Open = 100m, High = 110m, Low = 90m, Close = 105m, Volume = 1000UL };

            // Act & Assert
            Assert.Equal(20m, bar.Range); // High - Low
            Assert.Equal(5m, bar.Body); // |Close - Open|
            Assert.Equal(0.25m, bar.BodyPercent); // Body / Range
            Assert.True(bar.IsBullish);
            Assert.InRange(bar.Score, 50, 100);
        }
    }
}
