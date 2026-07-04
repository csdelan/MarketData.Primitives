using Core;
using MeshTransit.Contracts;

namespace MarketData.Workers;

/// <summary>
/// Projects background-job activity (from <see cref="JobRunRegistry"/>) onto the MeshTransit
/// liveness heartbeat: a <see cref="ServiceStatus"/> for at-a-glance health and a compact metadata
/// map carrying the per-job last-run digest. Wired into <c>AddMeshTransitHeartbeat</c> via the
/// publisher's per-tick <c>HealthSource</c> / <c>MetadataProvider</c> hooks, so the heartbeat always
/// reflects current job state without any push plumbing.
/// </summary>
public static class JobActivityHeartbeat
{
    /// <summary>
    /// <see cref="ServiceStatus.Degraded"/> when any job's most recent run failed; otherwise
    /// <see cref="ServiceStatus.Healthy"/> (including before any job has run).
    /// </summary>
    public static ServiceStatus DeriveStatus(JobRunRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var runs = registry.Snapshot();
        return runs.Values.Any(r => !r.Succeeded)
            ? ServiceStatus.Degraded
            : ServiceStatus.Healthy;
    }

    /// <summary>
    /// Populates the heartbeat metadata map with a bounded job-activity digest: rolled-up counts,
    /// the most recent failure, and one compact <c>job.&lt;key&gt;</c> entry per job. Kept small
    /// enough to ride every heartbeat tick.
    /// </summary>
    public static void PopulateMetadata(IDictionary<string, string> metadata, JobRunRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(registry);

        var runs = registry.Snapshot();
        metadata["jobs.count"] = runs.Count.ToString();

        if (runs.Count == 0)
            return;

        var failing = runs.Values.Where(r => !r.Succeeded).ToList();
        metadata["jobs.failing"] = failing.Count.ToString();

        if (failing.Count > 0)
        {
            var lastFail = failing.MaxBy(r => r.StartedAt)!;
            metadata["jobs.lastFail"] = $"{lastFail.JobKey}@{lastFail.StartedAt:HH:mm:ssZ}";
        }

        foreach (var run in runs.Values)
        {
            var outcome = run.Succeeded ? "ok" : "FAIL";
            metadata[$"job.{run.JobKey}"] = $"{outcome}@{run.StartedAt:HH:mm:ssZ}";
        }
    }
}
