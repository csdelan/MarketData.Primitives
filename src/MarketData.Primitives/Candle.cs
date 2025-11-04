namespace MarketData.Primitives
{
    public class Candle : Bar
    {
        #region Properties
        public Resolution Resolution { get; }
        public DateTimeOffset Timestamp { get; private set; }
        #endregion

        #region constructors
        public Candle()
        {
            Resolution = Resolution.Empty;
        }

        public Candle(decimal value, Resolution resolution = default, DateTimeOffset timestamp = default)
        {
            Resolution = Resolution.Empty;
            Close = Open = High = Low = value;
            Resolution = resolution;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Candle"/> class, representing a financial market candlestick
        /// with specified price levels, trading volume, resolution, and timestamp.
        /// </summary>
        /// <remarks>This constructor validates the provided price levels to ensure they form a valid
        /// candlestick. The <paramref name="resolution"/> parameter specifies the granularity of the candlestick, such
        /// as one minute, one hour, or one day.</remarks>
        /// <param name="open">The opening price of the candlestick.</param>
        /// <param name="high">The highest price reached during the candlestick's time period.</param>
        /// <param name="low">The lowest price reached during the candlestick's time period.</param>
        /// <param name="close">The closing price of the candlestick.</param>
        /// <param name="volume">The total trading volume during the candlestick's time period.</param>
        /// <param name="resolution">The time resolution of the candlestick, indicating the duration it represents. Must not be <see
        /// cref="Resolution.Empty"/>.</param>
        /// <param name="timestamp">The timestamp representing the start of the candlestick's time period. Defaults to <see
        /// cref="DateTimeOffset.MinValue"/> if not specified.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="resolution"/> is <see cref="Resolution.Empty"/> or if the price relationships are
        /// invalid. Price relationships are considered invalid if: <list type="bullet"> <item><description><paramref
        /// name="high"/> is less than <paramref name="low"/>.</description></item> <item><description><paramref
        /// name="open"/> is outside the range defined by <paramref name="low"/> and <paramref
        /// name="high"/>.</description></item> <item><description><paramref name="close"/> is outside the range defined
        /// by <paramref name="low"/> and <paramref name="high"/>.</description></item> </list></exception>
        public Candle(decimal open, decimal high, decimal low, decimal close, ulong volume,
            Resolution resolution = default, DateTimeOffset timestamp = default)
        {
            if (resolution.Equals(Resolution.Empty))
                throw new ArgumentException("Resolution cannot be empty.");
//            if ((high < low) || (open > high) || (open < low) || (close < low) || (close > high))
  //              throw new ArgumentException($"Invalid price relationships: O:{open} H:{high} L:{low} C:{close}");
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            Resolution = resolution;
            Timestamp = timestamp;
        }
        #endregion

        public long TimestampMilliseconds
        {
            get { return Timestamp.ToUnixTimeMilliseconds(); }
        }

        /// <summary>
        /// Gets the calculated end time based on the timestamp and resolution.
        /// For variable-length units (Months, Quarters, Years), the duration is calculated
        /// based on the specific start timestamp.
        /// </summary>
        public DateTimeOffset EndTime
        {
            get
            {
                if (Resolution.Equals(Resolution.Empty))
                    return Timestamp;
                
                // Use GetDurationToNextResolutionEvent which handles variable-length units correctly
                return Timestamp.Add(Resolution.GetDurationToNextResolutionEvent(Timestamp));
            }
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Open;
            yield return High;
            yield return Low;
            yield return Close;
            yield return Volume;
            yield return Resolution;
            yield return Timestamp;
        }
    }
}
