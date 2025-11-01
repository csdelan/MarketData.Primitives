using System.Collections;
using System.ComponentModel;

namespace MarketData.Primitives.Models
{
    public class CandleSeries : INotifyPropertyChanged, IEnumerable<Candle>
    {
        protected readonly List<Candle> _candles = new List<Candle>();
        public event PropertyChangedEventHandler? PropertyChanged;
        public IReadOnlyList<Candle> Candles => _candles.AsReadOnly();

        // Cached values
        protected decimal _high = decimal.MinValue;
        protected decimal _low = decimal.MaxValue;
        protected ulong _volume;

        public CandleSeries() { }

        public CandleSeries(IEnumerable<Candle> candles)
        {
            AppendCandles(candles);
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        public virtual void AppendCandle(Candle candle, bool notify = true)
        {
            if (candle.Resolution.IsEmpty)
                throw new ArgumentException("Candle resolution cannot be empty.");
            if (_candles.Any() && !candle.Resolution.Equals(Resolution))
                throw new ArgumentException("Candle resolution must match series resolution.");
            if (_candles.Any() && candle.Timestamp <= _candles.Last().Timestamp)
                throw new ArgumentException("Candle timestamp must be greater than the previous candle.");

            _candles.Add(candle);
            _high = Math.Max(_high, candle.High);
            _low = Math.Min(_low, candle.Low);
            _volume += candle.Volume;

            if (notify)
                OnPropertyChanged(nameof(Candles));
        }

        public IEnumerator<Candle> GetEnumerator() => _candles.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public decimal Open => _candles.Any() ? _candles[0].Open : throw new InvalidOperationException("Series is empty.");
        public decimal High => _candles.Any() ? _high : throw new InvalidOperationException("Series is empty.");
        public decimal Low => _candles.Any() ? _low : throw new InvalidOperationException("Series is empty.");
        public decimal Close => _candles.Any() ? _candles[_candles.Count - 1].Close : throw new InvalidOperationException("Series is empty.");
        public ulong Volume => _candles.Any() ? _volume : throw new InvalidOperationException("Series is empty.");

        /// <summary>
        /// Calculate one composite candle from the list of individual candles
        /// </summary>
        /// <returns>A single candle representing the entire collection of candles</returns>
        public Candle Consolidate()
        {
            if (!_candles.Any())
                throw new InvalidOperationException("Cannot consolidate an empty series.");
            var newResolution = new Resolution((uint)_candles.Count * Resolution.Count, Resolution.Unit);
            return new Candle(Open, High, Low, Close, Volume, newResolution, _candles[0].Timestamp);
        }

        public Resolution Resolution => _candles.Any() ? _candles[0].Resolution : throw new InvalidOperationException("Series is empty.");

        public Candle GetCandlestick(DateTimeOffset timestamp)
        {
            return this.FirstOrDefault(c => c.Timestamp == timestamp) ?? throw new InvalidOperationException("Candle not found.");
        }

        public decimal GetGap(Candle candle)
        {
            var index = _candles.IndexOf(candle);
            if (index <= 0)
                return 0;
            var previous = _candles[index - 1];
            return candle.Open - previous.Close;
        }

        public CandleSeries CopyRange(int startIndex, int endIndex)
        {
            if (startIndex < 0 || endIndex >= _candles.Count || startIndex > endIndex)
                throw new ArgumentException("Invalid index range.");
            return new CandleSeries(_candles.GetRange(startIndex, endIndex - startIndex + 1));
        }
    }
}
