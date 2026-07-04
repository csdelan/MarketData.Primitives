namespace MarketData.Primitives.Sessions;

/// <summary>
/// Session liquidity level, rated according to what is currently open in the market.
/// When the entire market is open, including full stock trading, it is full liquidity.
/// </summary>
public enum SessionLiquidityLevel
{
    /// <summary>Markets are closed; no phase is active.</summary>
    None = 0,

    /// <summary>Reduced volume typical of the pre-market and post-market sessions.</summary>
    Reduced,

    /// <summary>Very light overnight-futures volume.</summary>
    Light,

    /// <summary>The full regular session is open.</summary>
    Full
}
