using System.ComponentModel.DataAnnotations;

namespace MarketData.Workers;

/// <summary>
/// A single job-to-trigger binding read from configuration. <see cref="JobKey"/> must match an
/// <c>IBackgroundJob.Key</c> registered in the host.
/// </summary>
public sealed class JobSchedule
{
    /// <summary>The <c>IBackgroundJob.Key</c> this schedule fires.</summary>
    [Required]
    public string JobKey { get; set; } = string.Empty;

    /// <summary>The trigger kind.</summary>
    public ScheduleTrigger Trigger { get; set; } = ScheduleTrigger.IntervalAlways;

    /// <summary>Grid size for <see cref="ScheduleTrigger.IntervalAlways"/> / <see cref="ScheduleTrigger.EveryNMinutesDuringMarketHours"/>.</summary>
    [Range(1, 1440)]
    public int IntervalMinutes { get; set; } = 5;

    /// <summary>Cron expression for <see cref="ScheduleTrigger.Cron"/> (Hangfire/NCrontab syntax).</summary>
    public string? Cron { get; set; }

    /// <summary>Whether this schedule is active.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Static parameters passed to the job's <c>JobExecutionContext</c> on every fire.</summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}
