using Core;
using Microsoft.Extensions.Logging;

namespace MarketData.ServiceWorkers.Jobs;

/// <summary>
/// Minimal sample job: emits a structured "hello" log line. Demonstrates the simplest possible
/// <see cref="IBackgroundJob"/> and the clock-independent <c>IntervalAlways</c> trigger.
/// </summary>
public sealed class HelloWorldJob : IBackgroundJob
{
    public const string JobKey = "hello-world";

    private readonly ILogger<HelloWorldJob> _logger;

    public HelloWorldJob(ILogger<HelloWorldJob> logger) => _logger = logger;

    public string Key => JobKey;

    public Task ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Hello, world! Job {JobId} scheduled at {ScheduledAt} with {ParameterCount} parameter(s).",
            context.JobId, context.ScheduledAt, context.Parameters.Count);

        return Task.CompletedTask;
    }
}
