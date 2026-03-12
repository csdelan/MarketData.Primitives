namespace MarketData.Primitives
{
    /// <summary>
    /// Minimal contract descriptor shared between brokerage and securities layers.
    /// Provides the metadata needed to route an order without a direct dependency on MarketData.Securities.
    /// </summary>
    public interface IContractDescriptor
    {
        /// <summary>Full brokerage symbol (e.g., "AAPL", "AAPL  260320C00250000", "/ESM6").</summary>
        string Symbol { get; }

        /// <summary>Root underlying symbol (e.g., "AAPL", "ES").</summary>
        string UnderlyingSymbol { get; }

        /// <summary>Canonical asset taxonomy.</summary>
        SecurityType SecurityType { get; }

        /// <summary>Contract multiplier (1 for equity, 100 for standard options, 50 for /ES, etc.).</summary>
        decimal Multiplier { get; }
    }
}
