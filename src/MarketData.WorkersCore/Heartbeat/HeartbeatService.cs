using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketData.Workers;

/// <summary>
/// Periodically publishes a heartbeat event carrying a summary of background-job activity (last run
/// time and outcome per job) so an operator/eventing consumer can confirm the worker is alive and
/// see at a glance what each job last did.
/// </summary>
public sealed class HeartbeatService : BackgroundService
{
    private readonly IEventPublisher _events;
    private readonly JobRunRegistry _registry;
    private readonly TimeProvider _time;
    private readonly ServiceWorkerOptions _options;
    private readonly ILogger<HeartbeatService> _logger;

    public HeartbeatService(
        IEventPublisher events,
        JobRunRegistry registry,
        TimeProvider time,
        IOptions<ServiceWorkerOptions> options,
        ILogger<HeartbeatService> logger)
    {
        _events = events;
        _registry = registry;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.HeartbeatInterval, _time);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await _events.PublishAsync(WorkerEvents.Heartbeat(_options.ServiceName, BuildSummary()), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }

    private string BuildSummary()
    {
        var runs = _registry.Snapshot();
        if (runs.Count == 0)
            return "no jobs have run yet";

        var sb = new StringBuilder();
        sb.Append(runs.Count).Append(" job(s): ");
        sb.AppendJoin(", ", runs.Values.Select(r =>
            $"{r.JobKey}={(r.Succeeded ? "ok" : "FAIL")}@{r.StartedAt:HH:mm:ssZ}"));

        return sb.ToString();
    }
}
