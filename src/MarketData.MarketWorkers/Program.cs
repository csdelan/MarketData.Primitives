using Hangfire;
using MarketData.Workers;
using MarketData.MarketWorkers.Jobs;
using Microsoft.Extensions.Options;
using Serilog;

// Bootstrap logger so startup failures are logged before configuration is read.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, cfg) => cfg
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services
        .AddServiceWorkerCore(builder.Configuration)
        .AddWorkerJobs(builder.Configuration);

    var app = builder.Build();

    var workerOptions = app.Services.GetRequiredService<IOptions<ServiceWorkerOptions>>().Value;

    app.UseSerilogRequestLogging();

    // Hangfire monitoring dashboard — only on the designated process (ExposeHangfireDashboard: true).
    // All processes share the same SQLite store, so any one of them sees the full job history.
    // Set ExposeHangfireDashboard: false in appsettings.json for worker-only instances.
    if (workerOptions.ExposeHangfireDashboard)
    {
        app.UseHangfireDashboard(workerOptions.DashboardPath, new DashboardOptions
        {
            Authorization = [new AllowAllDashboardAuthorizationFilter()],
        });
    }

    // Dev convenience: manually enqueue a job by key without waiting for its schedule. Only mapped
    // in Development so a deployed clone of this template doesn't ship an unauthenticated way to
    // trigger arbitrary registered jobs in production.
    if (app.Environment.IsDevelopment())
    {
        app.MapPost("/run/{jobKey}", (string jobKey, IBackgroundJobClient jobs) =>
        {
            var id = jobs.Enqueue<JobDispatcher>(d => d.RunAsync(jobKey, null));
            return Results.Accepted($"/hangfire/jobs/details/{id}", new { jobKey, hangfireJobId = id });
        });
    }

    app.MapGet("/", () => Results.Ok(new
    {
        service = workerOptions.ServiceName,
        dashboard = workerOptions.DashboardPath,
    }));

    Log.Information("Starting {ServiceName}", workerOptions.ServiceName);
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service worker host terminated unexpectedly");
    // Non-zero exit so process supervisors (systemd, Windows service manager, container
    // orchestrators) see the failure instead of a clean-looking exit code 0.
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
