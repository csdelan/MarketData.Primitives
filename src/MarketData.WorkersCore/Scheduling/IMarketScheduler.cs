namespace MarketData.Workers;

/// <summary>
/// Computes the next-fire instant for a <see cref="JobSchedule"/>, resolving market-relative triggers
/// against the venue calendar. Exposed (rather than buried in the hosted service) so the timing logic
/// can be unit-tested deterministically with a manual clock.
/// </summary>
public interface IMarketScheduler
{
    /// <summary>The next UTC instant <paramref name="schedule"/> should fire, strictly after <paramref name="fromUtc"/>.</summary>
    DateTimeOffset ComputeNextFire(JobSchedule schedule, DateTimeOffset fromUtc);
}
