namespace MarketData.Primitives
{
    /// <summary>
    /// Pure helpers that combine pre-fetched candle data from two underlying
    /// symbols into a synthetic ratio (numerator / denominator) candle or series.
    /// No I/O, no symbol parsing — callers are responsible for fetching aligned
    /// data and passing it in.
    /// </summary>
    public static class RatioMath
    {
        /// <summary>
        /// Combines two same-resolution, same-timestamp candles into a synthetic
        /// ratio candle. Open/Close divide directly. High uses num.High/den.Low and
        /// Low uses num.Low/den.High to preserve "high is highest, low is lowest"
        /// for the ratio. Volume is set to 0 (undefined for derived instruments).
        /// </summary>
        /// <exception cref="ArgumentNullException">Either candle is null.</exception>
        /// <exception cref="ArgumentException">Resolutions or timestamps differ.</exception>
        /// <exception cref="DivideByZeroException">Any denominator OHLC field is 0.</exception>
        public static Candle CombineCandles(Candle numerator, Candle denominator)
        {
            ArgumentNullException.ThrowIfNull(numerator);
            ArgumentNullException.ThrowIfNull(denominator);

            if (!numerator.Resolution.Equals(denominator.Resolution))
                throw new ArgumentException(
                    $"Resolution mismatch: numerator={numerator.Resolution}, denominator={denominator.Resolution}.");

            if (numerator.Timestamp != denominator.Timestamp)
                throw new ArgumentException(
                    $"Timestamp mismatch: numerator={numerator.Timestamp:O}, denominator={denominator.Timestamp:O}.");

            if (denominator.Open == 0m || denominator.High == 0m || denominator.Low == 0m || denominator.Close == 0m)
                throw new DivideByZeroException("Denominator candle has a zero OHLC field; cannot compute ratio.");

            decimal open = numerator.Open / denominator.Open;
            decimal close = numerator.Close / denominator.Close;
            decimal high = numerator.High / denominator.Low;
            decimal low = numerator.Low / denominator.High;

            return new Candle(open, high, low, close, volume: 0UL,
                resolution: numerator.Resolution,
                timestamp: numerator.Timestamp);
        }

        /// <summary>
        /// Inner-joins two candle series on Timestamp, producing a sorted-ascending
        /// ratio series. Bars present on only one leg (e.g. when one symbol halts
        /// or has a gap) are dropped.
        /// </summary>
        /// <exception cref="ArgumentNullException">Either input is null.</exception>
        public static IReadOnlyList<Candle> CombineSeries(
            IReadOnlyList<Candle> numerator,
            IReadOnlyList<Candle> denominator)
        {
            ArgumentNullException.ThrowIfNull(numerator);
            ArgumentNullException.ThrowIfNull(denominator);

            var denByTimestamp = new Dictionary<DateTimeOffset, Candle>(denominator.Count);
            foreach (var d in denominator)
                denByTimestamp[d.Timestamp] = d;

            var result = new List<Candle>(Math.Min(numerator.Count, denominator.Count));
            foreach (var n in numerator)
            {
                if (!denByTimestamp.TryGetValue(n.Timestamp, out var d))
                    continue;

                if (d.Open == 0m || d.High == 0m || d.Low == 0m || d.Close == 0m)
                    continue;

                if (!n.Resolution.Equals(d.Resolution))
                    throw new ArgumentException(
                        $"Resolution mismatch at {n.Timestamp:O}: numerator={n.Resolution}, denominator={d.Resolution}.");

                result.Add(CombineCandles(n, d));
            }

            result.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            return result;
        }
    }
}
