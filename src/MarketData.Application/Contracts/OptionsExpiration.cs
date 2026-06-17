namespace MarketData.Application.Contracts;

/// <summary>
/// The "witching" character of an options expiration.
/// </summary>
public enum WitchingKind
{
    /// <summary>Ordinary (monthly) expiration.</summary>
    None = 0,

    /// <summary>Legacy triple witching: index futures, index options, and stock options.</summary>
    TripleWitching,

    /// <summary>Quad witching: adds single-stock futures. Quarterly third Fridays (Mar/Jun/Sep/Dec).</summary>
    QuadWitching
}

/// <summary>
/// An options-expiration date.
/// </summary>
/// <param name="Date">The effective expiration date (shifted earlier if the third Friday is a full-closure holiday).</param>
/// <param name="UnadjustedThirdFriday">The raw third Friday before any holiday adjustment.</param>
/// <param name="IsQuarterly">True for March/June/September/December expirations.</param>
/// <param name="Witching">The witching classification.</param>
public sealed record OptionsExpiration(
    DateOnly Date,
    DateOnly UnadjustedThirdFriday,
    bool IsQuarterly,
    WitchingKind Witching);
