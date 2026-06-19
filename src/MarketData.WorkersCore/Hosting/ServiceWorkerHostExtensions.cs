using Core;
using Core.BackgroundJobs;
using Core.Persistence;
using MarketData.Application.Calendar;
using MarketData.Application.Contracts;
using MarketData.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketData.Workers;

/// <summary>
/// Composition entry point for the <b>reusable</b> service-worker boilerplate. Wires options, clock,
/// market calendar, eventing, the persistence factory, Hangfire, dispatch, and the hosted
/// scheduler/heartbeat — everything that does <i>not</i> vary between worker projects.
/// <para>
/// It deliberately knows nothing about specific jobs. A host registers its jobs (and their clients,
/// stores, and options) separately via its own <c>AddWorkerJobs</c> extension, using
/// <see cref="JobRegistrationExtensions.AddBackgroundJob{TJob}"/>.
/// </para>
/// </summary>
public static class ServiceWorkerHostExtensions
{
    public static IServiceCollection AddServiceWorkerCore(this IServiceCollection services, IConfiguration configuration)
    {
        var hangfireOptions = AddOptions(services, configuration);

        // Clock: production uses the system clock. Swap for Core.ManualTimeProvider in sim/backtest.
        services.AddSingleton(TimeProvider.System);

        // Market calendar / context (reused from MarketData.Application; the scheduler's "when").
        services.AddSingleton<IMarketCalendar>(_ => new NyseMarketCalendar());
        services.AddSingleton<IMarketContextProvider>(sp =>
            new MarketContextProvider(sp.GetRequiredService<IMarketCalendar>(), sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton(sp =>
            new MarketClock(sp.GetRequiredService<IMarketContextProvider>(), sp.GetRequiredService<TimeProvider>()));

        // Eventing seam + job-run summary registry.
        services.AddSingleton<IEventPublisher, SerilogEventPublisher>();
        services.AddSingleton<JobRunRegistry>();

        // Dispatch: Core resolves/runs jobs by key; JobDispatcher adds eventing/result/registry.
        services.AddScoped<BackgroundJobExecutor>();
        services.AddScoped<JobDispatcher>();

        // Persistence factory (binds the "Persistence" config section). Individual document stores
        // are registered per job by the host's AddWorkerJobs.
        services.AddPersistence(configuration);

        services.AddServiceWorkerHangfire(hangfireOptions);

        // Hosted services: market-aware scheduler (also exposes IMarketScheduler) + heartbeat.
        services.AddSingleton<MarketScheduler>();
        services.AddSingleton<IMarketScheduler>(sp => sp.GetRequiredService<MarketScheduler>());
        services.AddHostedService(sp => sp.GetRequiredService<MarketScheduler>());
        services.AddHostedService<HeartbeatService>();

        return services;
    }

    private static HangfireOptions AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ServiceWorkerOptions>()
            .Bind(configuration.GetSection(ServiceWorkerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ScheduleOptions>()
            .Bind(configuration.GetSection(ScheduleOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var hangfireOptions = configuration
            .GetSection(HangfireOptions.SectionName)
            .Get<HangfireOptions>() ?? new HangfireOptions();

        services.AddOptions<HangfireOptions>()
            .Bind(configuration.GetSection(HangfireOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return hangfireOptions;
    }
}
