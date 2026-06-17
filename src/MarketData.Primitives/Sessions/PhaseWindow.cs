namespace MarketData.Primitives.Sessions;

/// <summary>
/// A single phase's local wall-clock window for a normal session day. Times are
/// venue-local <see cref="TimeOnly"/>. A window may cross midnight (Start &gt;= End),
/// which is used for the overnight futures phase.
/// </summary>
public readonly record struct PhaseWindow(MarketPhase Phase, TimeOnly Start, TimeOnly End)
{
    /// <summary>True when the window wraps past midnight (the end is at or before the start).</summary>
    public bool CrossesMidnight => End <= Start;
}
