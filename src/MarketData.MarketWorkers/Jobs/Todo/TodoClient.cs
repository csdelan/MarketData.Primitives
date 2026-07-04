using System.Net.Http.Json;
using Core.Json;

namespace MarketData.MarketWorkers.Jobs;

/// <summary>
/// Typed client for the jsonplaceholder todos endpoint. Registered via
/// <c>IHttpClientFactory</c> with a standard Polly resilience handler, so this class holds no
/// resilience logic of its own.
/// </summary>
public sealed class TodoClient(HttpClient http)
{
    public Task<TodoItem?> GetTodoAsync(int id, CancellationToken cancellationToken = default) =>
        http.GetFromJsonAsync<TodoItem>($"todos/{id}", CoreJson.Default, cancellationToken);
}
