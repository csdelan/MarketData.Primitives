using Core;

namespace MarketData.Primitives
{
    /// <summary>
    /// Represents an OHLC (Open, High, Low, Close) bar with volume, used for price data without a specific time resolution.
    /// </summary>
    public class Bar : ValueObject, IBar
    {
        /// <summary>
        /// Gets or sets the opening price of the asset.
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// Gets or sets the highest price of the asset during the time period.
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Gets or sets the lowest value recorded during the specified period.
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Gets or sets the closing price of the asset.
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// Gets or sets the volume level.
        /// </summary>
        public ulong Volume { get; set; }

        #region Computed Properties
        public decimal Range => Math.Abs(High - Low);
        public decimal Body => Math.Abs(Close - Open);

        /// <summary>
        /// Gets the percentage of the candle's body relative to its total range.
        /// </summary>
        public decimal BodyPercent => Range == 0 ? 0 : Body / Range;
        public decimal WickToBodyRatio => Body == 0 ? 0 : (LowerWick + UpperWick) / Body;
        public decimal WickPercent => Range == 0 ? 0 : (LowerWick + UpperWick) / Range;
        public decimal LowerWick => Math.Min(Open, Close) - Low;
        public decimal LowerWickPercent => Range == 0 ? 0 : LowerWick / Range;
        public decimal UpperWick => High - Math.Max(Open, Close);
        public decimal UpperWickPercent => Range == 0 ? 0 : UpperWick / Range;
        public decimal WickRatio => LowerWick == 0 ? 0 : UpperWick / LowerWick;
        public bool IsBullish { get { return Close > Open; } }
        public bool IsBearish { get { return Close < Open; } }

        /// <summary>
        /// Calculates a score for the candlestick based on Japanese candlestick analysis.
        /// The score ranges from 0 (extremely bearish) to 100 (extremely bullish).
        /// </summary>
        public int Score
        {
            get
            {
                const decimal bodyWeight = 0.6m; // Emphasize body size
                const decimal wickWeight = 0.4m; // Secondary emphasis on wick
                decimal score;

                if (IsBullish)
                {
                    // Bullish: Larger body and longer lower wick increase score
                    score = 50 + (BodyPercent * bodyWeight + LowerWickPercent * wickWeight) * 50;
                }
                else if (IsBearish)
                {
                    // Bearish: Larger body and longer upper wick decrease score
                    score = 50 - (BodyPercent * bodyWeight + UpperWickPercent * wickWeight) * 50;
                }
                else
                {
                    // Neutral (e.g., doji with Open == Close)
                    score = 50;
                }

                return (int)score;
            }
        }

        /// <summary>
        /// Does the candle represent a Doji pattern?
        /// </summary>
        /// <param name="threshold">threshold multiplier for body</param>
        /// <returns>true/false</returns>
        public bool IsDoji(decimal threshold = 0.05m) => Body <= threshold * Range;
        #endregion

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Open;
            yield return High;
            yield return Low;
            yield return Close;
            yield return Volume;
        }
    }
}
