using System.Text.Json;
using Core.Json;
using Hangfire;
using MarketData.Application.Contracts;
using MarketData.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketData.Workers;

/// <summary>
/// Hosted service that owns the market-aware "when": for each configured <see cref="JobSchedule"/>
/// it computes the next-fire instant from the venue calendar, waits on the injected
/// <see cref="TimeProvider"/> (so the timeline is backtest-drivable under a manual clock), then
/// enqueues the run into Hangfire. Hangfire owns execution, retries, and the dashboard.
/// </summary>
public sealed class MarketScheduler : BackgroundService, IMarketScheduler
{
    private readonly IMarketContextProvider _provider;
    private readonly MarketClock _clock;
    private readonly TimeProvider _time;
    private readonly IBackgroundJobClient _jobs;
    private readonly IRecurringJobManager _recurring;
    private readonly ScheduleOptions _options;
    private readonly ILogger<MarketScheduler> _logger;

    public MarketScheduler(
        IMarketContextProvider provider,
        MarketClock clock,
        TimeProvider time,
        IBackgroundJobClient jobs,
        IRecurringJobManager recurring,
        IOptions<ScheduleOptions> options,
        ILogger<MarketScheduler> logger)
    {
        _provider = provider;
        _clock = clock;
        _time = time;
        _jobs = jobs;
        _recurring = recurring;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timerLoops = new List<Task>();

        foreach (var schedule in _options.Jobs.Where(s => s.Enabled))
        {
            if (schedule.Trigger == ScheduleTrigger.Cron)
            {
                RegisterCron(schedule);
                continue;
            }

            _logger.LogInformation(
                "Scheduling job {JobKey} ({Trigger}); first fire at {NextFire:o}.",
                schedule.JobKey, schedule.Trigger, ComputeNextFire(schedule, _time.GetUtcNow()));

            timerLoops.Add(RunScheduleLoopAsync(schedule, stoppingToken));
        }

        await Task.WhenAll(timerLoops);
    }

    private void RegisterCron(JobSchedule schedule)
    {
        if (string.IsNullOrWhiteSpace(schedule.Cron))
        {
            _logger.LogWarning("Cron schedule for {JobKey} has no expression; skipping.", schedule.JobKey);
            return;
        }

        var parametersJson = Serialize(schedule.Parameters);
        _recurring.AddOrUpdate<JobDispatcher>(
            $"{schedule.JobKey}:cron",
            d => d.RunAsync(schedule.JobKey, parametersJson),
            schedule.Cron);

        _logger.LogInformation("Registered recurring job {JobKey} with cron '{Cron}'.", schedule.JobKey, schedule.Cron);
    }

    private async Task RunScheduleLoopAsync(JobSchedule schedule, CancellationToken stoppingToken)
    {
        var parametersJson = Serialize(schedule.Parameters);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = _time.GetUtcNow();
            var nextFire = ComputeNextFire(schedule, now);
            var delay = nextFire - now;
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;

            try
            {
                await Task.Delay(delay, _time, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _jobs.Enqueue<JobDispatcher>(d => d.RunAsync(schedule.JobKey, parametersJson));
            _logger.LogDebug("Enqueued {JobKey} (fired at {Fired:o}).", schedule.JobKey, nextFire);
        }
    }

    /// <inheritdoc />
    public DateTimeOffset ComputeNextFire(JobSchedule schedule, DateTimeOffset fromUtc) => schedule.Trigger switch
    {
        ScheduleTrigger.IntervalAlways => fromUtc + TimeSpan.FromMinutes(schedule.IntervalMinutes),
        ScheduleTrigger.MarketOpen => _clock.NextMarketOpen(),
        ScheduleTrigger.MarketClose => _provider.CurrentOrNextRegularCloseUtc(fromUtc) ?? _clock.NextMarketOpen(),
        ScheduleTrigger.EveryNMinutesDuringMarketHours => NextMarketGridFire(fromUtc, schedule.IntervalMinutes),
        _ => fromUtc + TimeSpan.FromMinutes(schedule.IntervalMinutes),
    };

    /// <summary>Next N-minute grid boundary that falls inside a regular session.</summary>
    private DateTimeOffset NextMarketGridFire(DateTimeOffset fromUtc, int intervalMinutes)
    {
        var candidate = GridCeiling(fromUtc, intervalMinutes, inclusive: false);

        // Bounded search: a candidate is either already in-session, or we jump to the next open and
        // align to the grid there. Two iterations suffice in practice; cap to stay safe.
        for (var i = 0; i < 8; i++)
        {
            if (_provider.IsRegularSessionOpen(candidate))
                return candidate;

            var nextOpen = _provider.NextRegularOpenUtc(candidate);
            if (nextOpen is null)
                return candidate; // provider reports currently-open; fire now.

            candidate = GridCeiling(nextOpen.Value, intervalMinutes, inclusive: true);
        }

        return candidate;
    }

    /// <summary>Rounds an instant up to the N-minute grid (anchored to the tick epoch, which keeps :00/:05/... alignment).</summary>
    private static DateTimeOffset GridCeiling(DateTimeOffset instant, int intervalMinutes, bool inclusive)
    {
        var step = TimeSpan.FromMinutes(intervalMinutes).Ticks;
        var ticks = instant.UtcTicks;
        var remainder = ticks % step;

        if (remainder == 0 && inclusive)
            return new DateTimeOffset(ticks, TimeSpan.Zero);

        var rounded = ticks - remainder + step;
        return new DateTimeOffset(rounded, TimeSpan.Zero);
    }

    private static string Serialize(Dictionary<string, string> parameters) =>
        JsonSerializer.Serialize(parameters, CoreJson.Default);
}
