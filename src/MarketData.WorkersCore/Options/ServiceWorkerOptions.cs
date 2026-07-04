using System.ComponentModel.DataAnnotations;

namespace MarketData.Workers;

/// <summary>
/// Top-level options for a service-worker host. Bound from the <c>ServiceWorker</c> configuration
/// section and validated at startup.
/// </summary>
public sealed class ServiceWorkerOptions
{
    public const string SectionName = "ServiceWorker";

    /// <summary>Logical name of this worker host; stamped onto events and heartbeats.</summary>
    [Required]
    public string ServiceName { get; set; } = "MarketData.MarketWorkers";

    /// <summary>Venue whose calendar drives market-relative schedules (e.g. <c>US-EQ</c>).</summary>
    [Required]
    public string VenueId { get; set; } = "US-EQ";

    /// <summary>
    /// MeshTransit liveness-heartbeat settings. The host advertises uptime/health on the mesh
    /// via these; see <see cref="HeartbeatSettings"/>.
    /// </summary>
    public HeartbeatSettings Heartbeat { get; set; } = new();

    /// <summary>Path the Hangfire dashboard is mounted at.</summary>
    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// When <c>true</c> (the default), this process serves the Hangfire dashboard at
    /// <see cref="DashboardPath"/>. Set to <c>false</c> on worker instances that should run jobs
    /// but not expose a dashboard UI — designate exactly one process (or a standalone dashboard
    /// app) as the single entry point.
    /// </summary>
    public bool ExposeHangfireDashboard { get; set; } = true;
}

/// <summary>
/// Settings for the MeshTransit liveness heartbeat this host broadcasts. The heartbeat is a
/// machine-readable uptime/health signal (instance id, sequence, status, uptime) on the reserved
/// <c>_mt.heartbeat.&lt;service&gt;</c> topic — distinct from the domain job-lifecycle events emitted
/// through <c>IEventPublisher</c>. A central monitor consumes these via MeshTransit's
/// <c>HeartbeatWatcher</c>.
/// </summary>
public sealed class HeartbeatSettings
{
    /// <summary>
    /// PUB socket bind address this process broadcasts heartbeats on, e.g. <c>tcp://*:9101</c>.
    /// Must be unique per process on a host.
    /// </summary>
    public string EventEndpoint { get; set; } = "tcp://*:9101";

    /// <summary>
    /// Heartbeat cadence in milliseconds (default 5000). Death is detected by the watcher after
    /// roughly <c>IntervalMs × missTolerance</c>.
    /// </summary>
    public int IntervalMs { get; set; } = 5000;
}
