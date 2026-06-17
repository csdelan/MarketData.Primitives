using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Storage.SQLite;
using Microsoft.Extensions.DependencyInjection;

namespace MarketData.Workers;

/// <summary>
/// Hangfire wiring for the worker host: SQLite persistent storage shared across all worker
/// processes on the machine (path from <see cref="HangfireOptions.DbPath"/>), plus the processing
/// server. Swap <see cref="UseSQLiteStorage"/> for a Mongo/SQL call to change the storage backend.
/// </summary>
public static class HangfireSetup
{
    public static IServiceCollection AddServiceWorkerHangfire(
        this IServiceCollection services,
        HangfireOptions options)
    {
        // SQLite won't create parent directories — ensure the path exists first.
        var dir = Path.GetDirectoryName(options.DbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSQLiteStorage(options.DbPath));

        services.AddHangfireServer();
        return services;
    }
}

/// <summary>
/// Dev-only dashboard authorization that permits every request. Replace with a real filter (e.g.
/// <c>LocalRequestsOnlyAuthorizationFilter</c> or auth-backed) before exposing the dashboard.
/// </summary>
public sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
