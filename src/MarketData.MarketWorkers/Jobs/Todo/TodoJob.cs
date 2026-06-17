using Core;
using Microsoft.Extensions.Logging;

namespace MarketData.ServiceWorkers.Jobs;

/// <summary>
/// Sample worker job: fetches a to-do item from jsonplaceholder and saves it to the document
/// store. Demonstrates the IHttpClientFactory + IDocumentStore seams without domain complexity.
/// </summary>
public sealed class TodoJob : IBackgroundJob
{
    public const string JobKey = "fetch-todo";

    private readonly TodoClient _client;
    private readonly IDocumentStore<TodoItem> _store;
    private readonly ILogger<TodoJob> _logger;

    public TodoJob(TodoClient client, IDocumentStore<TodoItem> store, ILogger<TodoJob> logger)
    {
        _client = client;
        _store = store;
        _logger = logger;
    }

    public string Key => JobKey;

    public async Task ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var todo = await _client.GetTodoAsync(id: 2, cancellationToken);
        if (todo is null) return;

        await _store.SaveAsync(todo, cancellationToken);
        _logger.LogInformation(
            "Saved todo {TodoId}: {Title} (completed={Completed}, job {JobId})",
            todo.TodoId, todo.Title, todo.Completed, context.JobId);
    }
}
