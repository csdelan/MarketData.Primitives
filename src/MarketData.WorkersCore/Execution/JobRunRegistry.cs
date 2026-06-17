using System.Collections.Concurrent;

namespace MarketData.Workers;

/// <summary>
/// In-memory record of the most recent <see cref="JobResult"/> per job key. Read by the heartbeat
/// service to build a summary of worker activity. Singleton; thread-safe.
/// </summary>
public sealed class JobRunRegistry
{
    private readonly ConcurrentDictionary<string, JobResult> _lastRuns = new();

    public void Record(JobResult result) => _lastRuns[result.JobKey] = result;

    public IReadOnlyDictionary<string, JobResult> Snapshot() =>
        new Dictionary<string, JobResult>(_lastRuns);
}
