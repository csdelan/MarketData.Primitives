using Core.Persistence;
using MarketData.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketData.MarketWorkers.Jobs;

/// <summary>
/// Registers this host's jobs and their job-specific dependencies (HTTP clients, document stores).
/// This is the file that varies from one worker project to the next — adding a job means adding
/// its folder under <c>Jobs/</c> and one registration here.
/// </summary>
public static class JobRegistration
{
    public static IServiceCollection AddWorkerJobs(this IServiceCollection services, IConfiguration configuration)
    {
        // hello-world: no external dependencies.
        services.AddBackgroundJob<HelloWorldJob>();

        // fetch-todo: typed HTTP client (+ resilience) and document store.
        services.AddHttpClient<TodoClient>(http =>
                http.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/"))
            .AddStandardResilienceHandler();

        services.AddDocumentStore<TodoItem>(
            storeName: "Todo",
            collectionName: "todos",
            jsonSubDirectory: "todos");

        services.AddBackgroundJob<TodoJob>();

        return services;
    }
}
