namespace MarketData.Workers;

/// <summary>
/// Outcome of a single job run. <c>IBackgroundJob.ExecuteAsync</c> returns no value, so the result
/// captured by the host is success/failure plus timing and any thrown error.
/// </summary>
public sealed record JobResult
{
    public required string JobKey { get; init; }
    public required string JobId { get; init; }
    public required bool Succeeded { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? Error { get; init; }

    public static JobResult Success(string jobKey, string jobId, DateTimeOffset startedAt, TimeSpan duration) =>
        new() { JobKey = jobKey, JobId = jobId, Succeeded = true, StartedAt = startedAt, Duration = duration };

    public static JobResult Failure(string jobKey, string jobId, DateTimeOffset startedAt, TimeSpan duration, string error) =>
        new() { JobKey = jobKey, JobId = jobId, Succeeded = false, StartedAt = startedAt, Duration = duration, Error = error };
}
