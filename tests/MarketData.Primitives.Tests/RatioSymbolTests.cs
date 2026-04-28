namespace MarketData.Primitives.Tests
{
    public class RatioSymbolTests
    {
        [Fact]
        public void Parse_ValidSymbol_SplitsAndUppercases()
        {
            var ratio = RatioSymbol.Parse("spy/gld");

            Assert.Equal("SPY", ratio.Numerator);
            Assert.Equal("GLD", ratio.Denominator);
        }

        [Fact]
        public void Parse_TrimsWhitespaceAroundLegs()
        {
            var ratio = RatioSymbol.Parse("  spy  /  gld  ");

            Assert.Equal("SPY", ratio.Numerator);
            Assert.Equal("GLD", ratio.Denominator);
        }

        [Fact]
        public void Parse_NoSlash_Throws()
        {
            Assert.Throws<FormatException>(() => RatioSymbol.Parse("SPY"));
        }

        [Fact]
        public void TryParse_NoSlash_ReturnsFalse()
        {
            Assert.False(RatioSymbol.TryParse("SPY", out _));
        }

        [Theory]
        [InlineData("SPY/")]
        [InlineData("/GLD")]
        [InlineData(" / ")]
        [InlineData("/")]
        public void Parse_EmptyHalf_Throws(string symbol)
        {
            Assert.Throws<FormatException>(() => RatioSymbol.Parse(symbol));
        }

        [Fact]
        public void Parse_MultipleSlashes_Throws()
        {
            Assert.Throws<FormatException>(() => RatioSymbol.Parse("A/B/C"));
        }

        [Fact]
        public void TryParse_MultipleSlashes_ReturnsFalse()
        {
            Assert.False(RatioSymbol.TryParse("A/B/C", out _));
        }

        [Theory]
        [InlineData("SPY/GLD", true)]
        [InlineData("spy/gld", true)]
        [InlineData("  SPY/GLD  ", true)]
        [InlineData("SPY", false)]
        [InlineData("SPY/", false)]
        [InlineData("/GLD", false)]
        [InlineData("A/B/C", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void IsRatio_PassesAndRejects_ExpectedCases(string? symbol, bool expected)
        {
            Assert.Equal(expected, RatioSymbol.IsRatio(symbol));
        }

        [Fact]
        public void ToString_RoundTrips()
        {
            var ratio = RatioSymbol.Parse("spy/gld");

            Assert.Equal("SPY/GLD", ratio.ToString());

            var reparsed = RatioSymbol.Parse(ratio.ToString());
            Assert.Equal(ratio, reparsed);
        }

        [Fact]
        public void Constructor_RejectsLegContainingSlash()
        {
            Assert.Throws<ArgumentException>(() => new RatioSymbol("A/B", "C"));
            Assert.Throws<ArgumentException>(() => new RatioSymbol("A", "B/C"));
        }

        [Fact]
        public void Equality_SameLegs_AreEqual()
        {
            var a = RatioSymbol.Parse("SPY/GLD");
            var b = RatioSymbol.Parse("spy/gld");

            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equality_DifferentLegs_AreNotEqual()
        {
            var a = RatioSymbol.Parse("SPY/GLD");
            var b = RatioSymbol.Parse("SPY/SLV");

            Assert.NotEqual(a, b);
            Assert.True(a != b);
        }
    }
}
