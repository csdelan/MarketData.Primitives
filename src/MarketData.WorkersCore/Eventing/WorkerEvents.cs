using Core;

namespace MarketData.Workers;

/// <summary>
/// Factory for the <see cref="BaseEvent"/>s the worker host emits. Keeps the <c>Class</c>/<c>Subclass</c>
/// taxonomy in one place so downstream consumers can filter consistently. The owning service name is
/// carried in <see cref="BaseEvent.Context"/>.
/// </summary>
public static class WorkerEvents
{
    public const string JobClass = "BackgroundJob";

    public static BaseEvent JobStarted(string serviceName, string jobKey, string jobId) => new()
    {
        Name = $"{jobKey} started",
        Class = JobClass,
        Subclass = "Started",
        Context = serviceName,
        Value = jobKey,
        Priority = 3,
        EventStatus = EventStatus.Processing,
        Body = $"job={jobKey} id={jobId}",
    };

    public static BaseEvent JobFinished(string serviceName, JobResult result) => new()
    {
        Name = $"{result.JobKey} {(result.Succeeded ? "succeeded" : "failed")}",
        Class = JobClass,
        Subclass = result.Succeeded ? "Succeeded" : "Failed",
        Context = serviceName,
        Value = result.JobKey,
        Priority = result.Succeeded ? 3 : 1,
        EventStatus = result.Succeeded ? EventStatus.Completed : EventStatus.Unread,
        StartedWorkDateTime = result.StartedAt,
        CompletedWorkDateTime = result.StartedAt + result.Duration,
        Description = result.Error,
        Body = result.Succeeded
            ? $"job={result.JobKey} id={result.JobId} durationMs={result.Duration.TotalMilliseconds:F0}"
            : $"job={result.JobKey} id={result.JobId} durationMs={result.Duration.TotalMilliseconds:F0} error={result.Error}",
    };
}
