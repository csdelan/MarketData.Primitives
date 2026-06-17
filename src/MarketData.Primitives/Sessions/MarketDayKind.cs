namespace MarketData.Primitives.Sessions;

/// <summary>
/// Classification of a calendar date with respect to a venue's trading calendar.
/// </summary>
public enum MarketDayKind
{
    /// <summary>A normal full-length trading day.</summary>
    RegularDay = 0,

    /// <summary>A shortened trading day (early close).</summary>
    HalfDay,

    /// <summary>A Saturday or Sunday.</summary>
    Weekend,

    /// <summary>A full-closure holiday.</summary>
    Holiday
}
