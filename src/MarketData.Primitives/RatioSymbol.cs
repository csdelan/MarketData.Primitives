namespace MarketData.Primitives
{
    /// <summary>
    /// A synthetic symbol of the form "NUM/DEN" that represents the price of one
    /// underlying symbol divided by another. Used as a first-class symbol by
    /// quote providers via decorators, so strategies/indicators can consume
    /// ratios identically to ordinary symbols.
    /// </summary>
    public readonly struct RatioSymbol : IEquatable<RatioSymbol>
    {
        public string Numerator { get; }
        public string Denominator { get; }

        public RatioSymbol(string numerator, string denominator)
        {
            if (string.IsNullOrWhiteSpace(numerator))
                throw new ArgumentException("Numerator must be non-empty.", nameof(numerator));
            if (string.IsNullOrWhiteSpace(denominator))
                throw new ArgumentException("Denominator must be non-empty.", nameof(denominator));
            if (numerator.Contains('/') || denominator.Contains('/'))
                throw new ArgumentException("Ratio legs cannot themselves contain '/'.");

            Numerator = numerator.Trim().ToUpperInvariant();
            Denominator = denominator.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// True when <paramref name="symbol"/> contains exactly one '/' separating
        /// two non-empty halves.
        /// </summary>
        public static bool IsRatio(string? symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return false;

            var trimmed = symbol.Trim();
            int firstSlash = trimmed.IndexOf('/');
            if (firstSlash < 0)
                return false;
            if (trimmed.IndexOf('/', firstSlash + 1) >= 0)
                return false;

            var num = trimmed.Substring(0, firstSlash).Trim();
            var den = trimmed.Substring(firstSlash + 1).Trim();
            return num.Length > 0 && den.Length > 0;
        }

        public static RatioSymbol Parse(string symbol)
        {
            if (!TryParse(symbol, out var result))
                throw new FormatException($"'{symbol}' is not a valid ratio symbol. Expected 'NUM/DEN'.");
            return result;
        }

        public static bool TryParse(string? symbol, out RatioSymbol result)
        {
            result = default;
            if (!IsRatio(symbol))
                return false;

            var trimmed = symbol!.Trim();
            int slash = trimmed.IndexOf('/');
            var num = trimmed.Substring(0, slash).Trim();
            var den = trimmed.Substring(slash + 1).Trim();

            result = new RatioSymbol(num, den);
            return true;
        }

        public override string ToString() => $"{Numerator}/{Denominator}";

        public bool Equals(RatioSymbol other) =>
            string.Equals(Numerator, other.Numerator, StringComparison.Ordinal) &&
            string.Equals(Denominator, other.Denominator, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is RatioSymbol other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

        public static bool operator ==(RatioSymbol left, RatioSymbol right) => left.Equals(right);
        public static bool operator !=(RatioSymbol left, RatioSymbol right) => !left.Equals(right);
    }
}
