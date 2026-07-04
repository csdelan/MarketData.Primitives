using Core;
using Microsoft.Extensions.DependencyInjection;

namespace MarketData.Workers;

/// <summary>
/// Helper for registering an <see cref="IBackgroundJob"/> so it is resolvable both as the concrete
/// type (which <see cref="BackgroundJobExecutor"/> resolves in-scope) and as <see cref="IBackgroundJob"/>
/// (which builds the executor's key map) — backed by a single scoped instance.
/// </summary>
public static class JobRegistrationExtensions
{
    public static IServiceCollection AddBackgroundJob<TJob>(this IServiceCollection services)
        where TJob : class, IBackgroundJob
    {
        services.AddScoped<TJob>();
        services.AddScoped<IBackgroundJob>(sp => sp.GetRequiredService<TJob>());
        return services;
    }
}
