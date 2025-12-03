using MarketData.Primitives.Models;

namespace MarketData.Primitives
{
    public static class CandleSeriesExtensions
    {
        /// <summary>
        /// Returns a new <see cref="CandleSeries"/> sorted by <see cref="Candle.Timestamp"/> in ascending order.
        /// </summary>
        /// <param name="series">The source series to sort.</param>
        /// <param name="ascending">Ignored. Present only for signature/backward compatibility.</param>
        /// <returns>A new series containing the input candles ordered by timestamp. Returns an empty series if the source is empty.</returns>
        /// <remarks>
        /// This method always sorts in ascending order regardless of the <paramref name="ascending"/> value to maintain
        /// compatibility with previous signatures while enforcing a single canonical ordering.
        /// </remarks>
        public static CandleSeries Sorted(this CandleSeries series, bool ascending = true)
        {
            if (!series.Candles.Any())
                return new CandleSeries();

            var ordered = series.Candles.OrderBy(c => c.Timestamp);
            return new CandleSeries(ordered);
        }

        /// <summary>
        /// Trims the series to contain at most <paramref name="max"/> candles, keeping the most recent candles.
        /// </summary>
        /// <param name="series">The source series.</param>
        /// <param name="max">The maximum number of candles to retain. Must be non-negative.</param>
        /// <returns>
        /// A new series containing at most <paramref name="max"/> candles. If the source has fewer or equal candles,
        /// a copy of the source is returned. Returns an empty series when the source is empty.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="max"/> is negative.</exception>
        public static CandleSeries TrimToMax(this CandleSeries series, int max)
        {
            if (max < 0) throw new ArgumentOutOfRangeException(nameof(max));
            if (!series.Candles.Any()) return new CandleSeries();
            if (series.Candles.Count <= max) return new CandleSeries(series.Candles);
            var trimmed = series.Candles.Skip(series.Candles.Count - max);
            return new CandleSeries(trimmed);
        }

        /// <summary>
        /// Filters candles to those that fall within the specified market session window.
        /// </summary>
        /// <param name="series">The source series.</param>
        /// <param name="open">The session open time of day in <paramref name="tz"/>.</param>
        /// <param name="close">The session close time of day in <paramref name="tz"/>.</param>
        /// <param name="tz">The time zone in which the session times are defined.</param>
        /// <returns>A new series containing only candles within the session window.</returns>
        /// <remarks>
        /// For intraday data, the candle's duration (from its resolution) is used to determine whether the candle's
        /// time window is within the session. For non-intraday data, a zero duration is assumed and the candle timestamp
        /// itself is evaluated.
        /// </remarks>
        public static CandleSeries FilterToSession(this CandleSeries series, TimeSpan open, TimeSpan close, TimeZoneInfo tz)
        {
            if (!series.Candles.Any()) return new CandleSeries();
            var duration = series.Resolution.IsIntraday ? series.Resolution.GetTimeSpan() : TimeSpan.Zero;
            var filtered = series.Candles.Where(c => MarketHours.IsWithinSession(c.Timestamp, duration, open, close, tz));
            return new CandleSeries(filtered);
        }

        /// <summary>
        /// Filters candles to the NYSE regular trading session (09:30–16:00 Eastern Time).
        /// </summary>
        /// <param name="series">The source series.</param>
        /// <returns>A new series containing only candles within the NYSE regular session.</returns>
        public static CandleSeries FilterToNyseRegularSession(this CandleSeries series)
            => series.FilterToSession(MarketHours.NyseOpen, MarketHours.NyseClose, MarketHours.EasternTimeZone);

        /// <summary>
        /// Aggregates the source candles into the specified target <see cref="Resolution"/>.
        /// </summary>
        /// <param name="series">The source series.</param>
        /// <param name="target">The target resolution to aggregate to.</param>
        /// <returns>
        /// A new series of candles aligned to <paramref name="target"/> boundaries and sorted by timestamp.
        /// For each bucket, the Open is the first candle's open, High is the maximum high, Low is the minimum low,
        /// Close is the last candle's close, and Volume is the sum of volumes.
        /// Returns an empty series if the source is empty.
        /// </returns>
        /// <remarks>
        /// Each input candle is assigned to a bucket starting at <see cref="Resolution.GetLastEvent(DateTimeOffset)"/>
        /// for the candle timestamp in the <paramref name="target"/> resolution.
        /// </remarks>
        public static CandleSeries Aggregate(this CandleSeries series, Resolution target)
        {
            var source = series.Candles;
            if (source.Count == 0) return new CandleSeries();

            var buckets = new Dictionary<DateTimeOffset, (decimal Open, decimal High, decimal Low, decimal Close, ulong Volume)>();

            foreach (var c in source.OrderBy(c => c.Timestamp))
            {
                var bucketStart = target.GetLastEvent(c.Timestamp);

                if (!buckets.TryGetValue(bucketStart, out var agg))
                {
                    agg = (c.Open, c.High, c.Low, c.Close, c.Volume);
                }
                else
                {
                    var open = agg.Open;
                    var high = Math.Max(agg.High, c.High);
                    var low = Math.Min(agg.Low, c.Low);
                    var close = c.Close;
                    var vol = agg.Volume + c.Volume;
                    agg = (open, high, low, close, vol);
                }

                buckets[bucketStart] = agg;
            }

            var aggregated = buckets
                .Select(kvp =>
                {
                    var ts = kvp.Key;
                    var a = kvp.Value;
                    return new Candle(a.Open, a.High, a.Low, a.Close, a.Volume, target, ts);
                })
                .OrderBy(c => c.Timestamp);

            return new CandleSeries(aggregated);
        }
    }
}