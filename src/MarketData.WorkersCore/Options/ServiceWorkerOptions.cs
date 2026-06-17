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
    public string ServiceName { get; set; } = "MarketData.ServiceWorkers";

    /// <summary>Venue whose calendar drives market-relative schedules (e.g. <c>US-EQ</c>).</summary>
    [Required]
    public string VenueId { get; set; } = "US-EQ";

    /// <summary>How often the heartbeat service publishes a summary event.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Path the Hangfire dashboard is mounted at.</summary>
    public string DashboardPath { get; set; } = "/hangfire";
}
