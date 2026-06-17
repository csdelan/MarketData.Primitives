using Core.Persistence;
using MarketData.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MarketData.ServiceWorkers.Jobs;

/// <summary>
/// Registers this host's jobs and their job-specific dependencies (HTTP clients, document stores,
/// options). This is the file that varies from one worker project to the next — adding a job means
/// adding its folder under <c>Jobs/</c> and one registration here.
/// </summary>
public static class JobRegistration
{
    public static IServiceCollection AddWorkerJobs(this IServiceCollection services, IConfiguration configuration)
    {
        // hello-world: no external dependencies.
        services.AddBackgroundJob<HelloWorldJob>();

        // exchange-rates: typed HTTP client (+ resilience), document store, and options.
        services.AddOptions<TreasuryApiOptions>()
            .Bind(configuration.GetSection(TreasuryApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<TreasuryFiscalDataClient>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<TreasuryApiOptions>>().Value;
                http.BaseAddress = new Uri(options.BaseUrl);
            })
            // Polly v8 standard resilience: retry + timeout + circuit breaker.
            .AddStandardResilienceHandler();

        // IDocument overload: the key defaults to x => x.Id.
        services.AddDocumentStore<ExchangeRateSnapshot>(
            storeName: "ExchangeRate",
            collectionName: "exchange-rates",
            jsonSubDirectory: "exchange-rates");

        services.AddBackgroundJob<ExchangeRatesJob>();

        return services;
    }
}
