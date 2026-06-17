namespace MarketData.Application.Contracts;

/// <summary>
/// A named market holiday or early-close day.
/// </summary>
/// <param name="Date">The calendar date.</param>
/// <param name="Name">Human-readable name (e.g., "Christmas Day", "Day after Thanksgiving").</param>
/// <param name="IsEarlyClose">
/// False for a full closure; true for an early-close (half) day with a named reason.
/// </param>
public sealed record Holiday(DateOnly Date, string Name, bool IsEarlyClose);
