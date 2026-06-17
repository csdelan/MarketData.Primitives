using System.Text.Json;
using Core;
using Core.Json;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketData.Workers;

/// <summary>
/// The Hangfire job target. Wraps <see cref="BackgroundJobExecutor"/> (which resolves and runs the
/// <c>IBackgroundJob</c> by key) with the eventing/result/registry layer the executor itself does
/// not provide: publish started → time the run → capture a <see cref="JobResult"/> → record it →
/// publish finished.
/// </summary>
public sealed class JobDispatcher
{
    private readonly BackgroundJobExecutor _executor;
    private readonly IEventPublisher _events;
    private readonly JobRunRegistry _registry;
    private readonly TimeProvider _time;
    private readonly ILogger<JobDispatcher> _logger;
    private readonly string _serviceName;

    public JobDispatcher(
        BackgroundJobExecutor executor,
        IEventPublisher events,
        JobRunRegistry registry,
        TimeProvider time,
        IOptions<ServiceWorkerOptions> options,
        ILogger<JobDispatcher> logger)
    {
        _executor = executor;
        _events = events;
        _registry = registry;
        _time = time;
        _logger = logger;
        _serviceName = options.Value.ServiceName;
    }

    /// <summary>
    /// Runs a job by key. <paramref name="parametersJson"/> is a JSON object of string→string passed
    /// through to the job's <c>JobExecutionContext.Parameters</c>. Signature is kept Hangfire-friendly
    /// (primitive args only). The <see cref="DisplayNameAttribute"/> makes the Hangfire dashboard show
    /// the job key (argument <c>{0}</c>) instead of the generic <c>JobDispatcher.RunAsync</c>.
    /// </summary>
    [JobDisplayName("{0}")]
    public async Task RunAsync(string jobKey, string? parametersJson = null)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var startedAt = _time.GetUtcNow();
        var startTimestamp = _time.GetTimestamp();

        await _events.PublishAsync(WorkerEvents.JobStarted(_serviceName, jobKey, jobId));

        var parameters = Deserialize(parametersJson);

        JobResult result;
        try
        {
            await _executor.ExecuteAsync(jobKey, parameters);
            result = JobResult.Success(jobKey, jobId, startedAt, _time.GetElapsedTime(startTimestamp));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobKey} ({JobId}) failed", jobKey, jobId);
            result = JobResult.Failure(jobKey, jobId, startedAt, _time.GetElapsedTime(startTimestamp), ex.Message);
        }

        _registry.Record(result);
        await _events.PublishAsync(WorkerEvents.JobFinished(_serviceName, result));

        // Surface failure to Hangfire so its retry/monitoring sees it.
        if (!result.Succeeded)
            throw new JobExecutionFailedException(jobKey, result.Error ?? "unknown error");
    }

    private static Dictionary<string, string>? Deserialize(string? parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
            return null;

        return JsonSerializer.Deserialize<Dictionary<string, string>>(parametersJson, CoreJson.Default);
    }
}

/// <summary>Thrown to propagate a job failure to Hangfire after the failure event is published.</summary>
public sealed class JobExecutionFailedException(string jobKey, string error)
    : Exception($"Background job '{jobKey}' failed: {error}");
