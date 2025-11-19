using System.Buffers;
using System.Collections;
using System.ComponentModel;

namespace MarketData.Primitives.Models
{
    /// <summary>
    /// Represents a time-series collection of candlestick data with high-performance access patterns.
    /// Uses <see cref="ArrayPool{T}"/> for efficient memory management and provides both list-like and span-based access.
    /// </summary>
    /// <remarks>
    /// This class implements <see cref="IDisposable"/> and should be disposed when no longer needed to return
    /// pooled arrays. For performance-critical scenarios, use <see cref="AsSpan()"/> methods instead of the
    /// <see cref="Candles"/> property which creates defensive copies.
    /// </remarks>
    public class CandleSeries : INotifyPropertyChanged, IEnumerable<Candle>, IDisposable
    {
        private Candle[] _buffer;
        private int _count;
        private readonly ArrayPool<Candle> _pool;
        
        /// <summary>
        /// Occurs when a property value changes, particularly when candles are added to the series.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        // Cached values
        /// <summary>
        /// Cached highest price across all candles in the series.
        /// </summary>
        protected decimal _high = decimal.MinValue;
        
        /// <summary>
        /// Cached lowest price across all candles in the series.
        /// </summary>
        protected decimal _low = decimal.MaxValue;
        
        /// <summary>
        /// Cached cumulative volume across all candles in the series.
        /// </summary>
        protected ulong _volume;

        /// <summary>
        /// Initializes a new instance of the <see cref="CandleSeries"/> class with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the internal buffer. Default is 256.</param>
        /// <remarks>
        /// The internal buffer will automatically grow as needed when candles are appended.
        /// Choosing an appropriate initial capacity can reduce reallocations for known series sizes.
        /// </remarks>
        public CandleSeries(int initialCapacity = 256)
        {
            _pool = ArrayPool<Candle>.Shared;
            _buffer = _pool.Rent(initialCapacity);
            _count = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CandleSeries"/> class and populates it with the specified candles.
        /// </summary>
        /// <param name="candles">The collection of candles to add to the series.</param>
        /// <param name="initialCapacity">The initial capacity of the internal buffer. Default is 256.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if any candle has an empty resolution, mismatched resolution, or non-increasing timestamp.
        /// </exception>
        public CandleSeries(IEnumerable<Candle> candles, int initialCapacity = 256) : this(initialCapacity)
        {
            AppendCandles(candles);
        }

        /// <summary>
        /// Finalizer that returns the pooled array if not already disposed.
        /// </summary>
        ~CandleSeries()
        {
            if (_buffer != null)
                _pool.Return(_buffer);
        }

        /// <summary>
        /// Releases the pooled array back to the <see cref="ArrayPool{T}"/>.
        /// </summary>
        /// <remarks>
        /// After calling <see cref="Dispose"/>, the series should not be used anymore.
        /// This method suppresses finalization as cleanup has been performed.
        /// </remarks>
        public void Dispose()
        {
            if (_buffer != null)
            {
                _pool.Return(_buffer);
                _buffer = null!;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets a read-only list view of all candles in the series.
        /// </summary>
        /// <value>
        /// An <see cref="IReadOnlyList{T}"/> containing all candles in the series.
        /// </value>
        /// <remarks>
        /// <para>This property creates a defensive copy of the internal buffer on each access for backward compatibility.</para>
        /// <para>For performance-critical scenarios, use <see cref="AsSpan()"/> instead to avoid allocations.</para>
        /// </remarks>
        public IReadOnlyList<Candle> Candles => AsSpan().ToArray();

        /// <summary>
        /// Gets a read-only span over all candles in the series.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> containing all candles in the series.</returns>
        /// <remarks>
        /// This method provides zero-allocation access to the candle data and is preferred for
        /// performance-critical operations. The span is valid only while the series is not modified.
        /// </remarks>
        public ReadOnlySpan<Candle> AsSpan() => new ReadOnlySpan<Candle>(_buffer, 0, _count);

        /// <summary>
        /// Gets a read-only span over a range of candles.
        /// </summary>
        /// <param name="start">The zero-based starting index of the range.</param>
        /// <param name="length">The number of candles to include in the span.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> containing the specified range of candles.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="start"/> is negative or if <paramref name="start"/> + <paramref name="length"/> exceeds the series count.
        /// </exception>
        public ReadOnlySpan<Candle> AsSpan(int start, int length)
        {
            if (start < 0 || start + length > _count)
                throw new ArgumentOutOfRangeException();
            return new ReadOnlySpan<Candle>(_buffer, start, length);
        }

        /// <summary>
        /// Gets a span over the last N candles in the series.
        /// </summary>
        /// <param name="count">The number of candles to include, starting from the end of the series.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> containing the last <paramref name="count"/> candles.</returns>
        /// <remarks>
        /// If <paramref name="count"/> exceeds the number of candles in the series, all candles are returned.
        /// </remarks>
        public ReadOnlySpan<Candle> TakeLast(int count)
        {
            if (count > _count)
                count = _count;
            return new ReadOnlySpan<Candle>(_buffer, _count - count, count);
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Ensures the internal buffer has at least the required capacity, growing it if necessary.
        /// </summary>
        /// <param name="required">The minimum required capacity.</param>
        /// <remarks>
        /// When growing, the new size is at least double the current size or the required size, whichever is larger.
        /// The old buffer is returned to the pool after copying data to the new buffer.
        /// </remarks>
        private void EnsureCapacity(int required)
        {
            if (_buffer.Length >= required)
                return;

            int newSize = Math.Max(_buffer.Length * 2, required);
            Candle[] newBuffer = _pool.Rent(newSize);
            Array.Copy(_buffer, 0, newBuffer, 0, _count);
            _pool.Return(_buffer);
            _buffer = newBuffer;
        }

        /// <summary>
        /// Appends multiple candles to the series from an enumerable collection.
        /// </summary>
        /// <param name="candles">The collection of candles to append.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if any candle has an empty resolution, mismatched resolution with the series,
        /// or a timestamp that is not greater than the previous candle.
        /// </exception>
        /// <remarks>
        /// This method raises the <see cref="PropertyChanged"/> event once after all candles are added.
        /// </remarks>
        public virtual void AppendCandles(IEnumerable<Candle> candles)
        {
            bool anyAdded = false;
            foreach (var candle in candles)
            {
                AppendCandle(candle, false);
                anyAdded = true;
            }
            if (anyAdded)
                OnPropertyChanged(nameof(Candles));
        }

        /// <summary>
        /// Appends multiple candles to the series from a read-only span.
        /// </summary>
        /// <param name="candles">The span of candles to append.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if any candle has an empty resolution, mismatched resolution with the series,
        /// or a timestamp that is not greater than the previous candle.
        /// </exception>
        /// <remarks>
        /// This overload is optimized for span-based operations and raises the <see cref="PropertyChanged"/> 
        /// event once after all candles are added.
        /// </remarks>
        public virtual void AppendCandles(ReadOnlySpan<Candle> candles)
        {
            EnsureCapacity(_count + candles.Length);
            foreach (var candle in candles)
            {
                AppendCandle(candle, false);
            }
            if (candles.Length > 0)
                OnPropertyChanged(nameof(Candles));
        }

        /// <summary>
        /// Appends a single candle to the series.
        /// </summary>
        /// <param name="candle">The candle to append.</param>
        /// <param name="notify">If <c>true</c>, raises the <see cref="PropertyChanged"/> event. Default is <c>true</c>.</param>
        /// <exception cref="ArgumentException">
        /// Thrown in the following cases:
        /// <list type="bullet">
        /// <item><description>The candle's resolution is empty (<see cref="Resolution.IsEmpty"/>).</description></item>
        /// <item><description>The candle's resolution does not match the series resolution (for series with existing candles).</description></item>
        /// <item><description>The candle's timestamp is not greater than the previous candle's timestamp.</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// The first candle appended determines the resolution for the entire series.
        /// Subsequent candles must have strictly increasing timestamps and matching resolution.
        /// </remarks>
        public virtual void AppendCandle(Candle candle, bool notify = true)
        {
            if (candle.Resolution.IsEmpty)
                throw new ArgumentException("Candle resolution cannot be empty.");
            if (_count > 0 && !candle.Resolution.Equals(Resolution))
                throw new ArgumentException("Candle resolution must match series resolution.");
            if (_count > 0 && candle.Timestamp <= _buffer[_count - 1].Timestamp)
                throw new ArgumentException("Candle timestamp must be greater than the previous candle.");

            EnsureCapacity(_count + 1);
            _buffer[_count++] = candle;
            _high = Math.Max(_high, candle.High);
            _low = Math.Min(_low, candle.Low);
            _volume += candle.Volume;

            if (notify)
                OnPropertyChanged(nameof(Candles));
        }

        /// <summary>
        /// Returns an enumerator that iterates through the candles in the series.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> for the series.</returns>
        public IEnumerator<Candle> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return _buffer[i];
        }

        /// <summary>
        /// Returns an enumerator that iterates through the candles in the series.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> for the series.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Gets the number of candles in the series.
        /// </summary>
        /// <value>The total count of candles in the series.</value>
        public int Count => _count;

        /// <summary>
        /// Gets the opening price of the first candle in the series.
        /// </summary>
        /// <value>The opening price of the first candle.</value>
        /// <exception cref="InvalidOperationException">Thrown if the series is empty.</exception>
        public decimal Open => _count > 0 ? _buffer[0].Open : throw new InvalidOperationException("Series is empty.");
        
        /// <summary>
        /// Gets the highest price across all candles in the series.
        /// </summary>
        /// <value>The highest price value.</value>
        /// <exception cref="InvalidOperationException">Thrown if the series is empty.</exception>
        public decimal High => _count > 0 ? _high : throw new InvalidOperationException("Series is empty.");
        
        /// <summary>
        /// Gets the lowest price across all candles in the series.
        /// </summary>
        /// <value>The lowest price value.</value>
        /// <exception cref="InvalidOperationException">Thrown if the series is empty.</exception>
        public decimal Low => _count > 0 ? _low : throw new InvalidOperationException("Series is empty.");
        
        /// <summary>
        /// Gets the closing price of the last candle in the series.
        /// </summary>
        /// <value>The closing price of the last candle.</value>
        /// <exception cref="InvalidOperationException">Thrown if the series is empty.</exception>
        public decimal Close => _count > 0 ? _buffer[_count - 1].Close : throw new InvalidOperationException("Series is empty.");
        
        /// <summary>
        /// Gets the cumulative volume across all candles in the series.
        /// </summary>
        /// <value>The total trading volume.</value>
        /// <exception cref="InvalidOperationException">Thrown if the series is empty.</exception>
        public ulong Volume => _count > 0 ? _volume : throw new InvalidOperationException("Series is empty.");

        /// <summary>
        /// Consolidates all candles in the series into a single composite candle.
        /// </summary>
        /// <returns>
        /// A new <see cref="Candle"/> representing the consolidated data with the opening price of the first candle,
        /// closing price of the last candle, highest and lowest prices across all candles, and cumulative volume.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if the series is empty.</exception>
        /// <remarks>
        /// The resulting candle's resolution is calculated by multiplying the series resolution count by the number of candles.
        /// The timestamp is set to the timestamp of the first candle in the series.
        /// </remarks>
        public Candle Consolidate()
        {
            if (_count == 0)
                throw new InvalidOperationException("Cannot consolidate an empty series.");
            var newResolution = new Resolution((uint)_count * Resolution.Count, Resolution.Unit);
            return new Candle(Open, High, Low, Close, Volume, newResolution, _buffer[0].Timestamp);
        }

        /// <summary>
        /// Gets the resolution (timeframe) of the candles in the series.
        /// </summary>
        /// <value>The <see cref="Models.Resolution"/> of the series, determined by the first candle.</value>
        /// <exception cref="InvalidOperationException">Thrown if the series is empty.</exception>
        /// <remarks>
        /// All candles in a series must share the same resolution.
        /// </remarks>
        public Resolution Resolution => _count > 0 ? _buffer[0].Resolution : throw new InvalidOperationException("Series is empty.");

        /// <summary>
        /// Retrieves a candle from the series by its timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp of the candle to retrieve.</param>
        /// <returns>The <see cref="Candle"/> with the matching timestamp.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no candle with the specified timestamp is found.</exception>
        public Candle GetCandlestick(DateTimeOffset timestamp)
        {
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].Timestamp == timestamp)
                    return span[i];
            }
            throw new InvalidOperationException("Candle not found.");
        }

        /// <summary>
        /// Calculates the gap between a candle's opening price and the previous candle's closing price.
        /// </summary>
        /// <param name="candle">The candle to calculate the gap for.</param>
        /// <returns>
        /// The price gap (candle.Open - previousCandle.Close) if the candle is found and is not the first candle;
        /// otherwise, returns 0.
        /// </returns>
        /// <remarks>
        /// A positive value indicates a gap up (opening above the previous close),
        /// while a negative value indicates a gap down (opening below the previous close).
        /// Returns 0 if the candle is the first in the series or not found.
        /// </remarks>
        public decimal GetGap(Candle candle)
        {
            var span = AsSpan();
            for (int i = 1; i < span.Length; i++)
            {
                if (span[i].Equals(candle))
                    return candle.Open - span[i - 1].Close;
            }
            return 0;
        }

        /// <summary>
        /// Creates a new <see cref="CandleSeries"/> containing a copy of a specified range of candles.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the range (inclusive).</param>
        /// <param name="endIndex">The zero-based ending index of the range (inclusive).</param>
        /// <returns>A new <see cref="CandleSeries"/> containing the candles in the specified range.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="startIndex"/> is negative, <paramref name="endIndex"/> is greater than or equal to the series count,
        /// or <paramref name="startIndex"/> is greater than <paramref name="endIndex"/>.
        /// </exception>
        /// <remarks>
        /// Both <paramref name="startIndex"/> and <paramref name="endIndex"/> are inclusive.
        /// The resulting series maintains all cached statistics for the copied range.
        /// </remarks>
        public CandleSeries CopyRange(int startIndex, int endIndex)
        {
            if (startIndex < 0 || endIndex >= _count || startIndex > endIndex)
                throw new ArgumentException("Invalid index range.");
            
            var result = new CandleSeries(endIndex - startIndex + 1);
            var sourceSpan = AsSpan(startIndex, endIndex - startIndex + 1);
            result.AppendCandles(sourceSpan);
            return result;
        }
    }
}
