using MarketData.Primitives.Sessions;

namespace MarketData.Application.Contracts;

/// <summary>
/// Live status of a single phase relative to a given instant.
/// </summary>
/// <param name="Phase">The phase this status describes.</param>
/// <param name="IsActive">Whether the phase is currently active.</param>
/// <param name="StartUtc">Start of the current occurrence (if active) or the next occurrence (if not).</param>
/// <param name="EndUtc">End of the current occurrence (if active) or the next occurrence (if not).</param>
/// <param name="Elapsed">Time since the phase started; null when not active.</param>
/// <param name="Remaining">Time until the active phase ends; null when not active.</param>
/// <param name="TimeUntilStart">Time until the next occurrence begins; null when active.</param>
public sealed record PhaseStatus(
    MarketPhase Phase,
    bool IsActive,
    DateTimeOffset? StartUtc,
    DateTimeOffset? EndUtc,
    TimeSpan? Elapsed,
    TimeSpan? Remaining,
    TimeSpan? TimeUntilStart);
