using System.Text.Json.Serialization;
using Core;

namespace MarketData.MarketWorkers.Jobs;

/// <summary>
/// A single to-do item from jsonplaceholder.typicode.com, stored as an <see cref="IDocument"/>
/// keyed by the API's integer id. Demonstrates the typed-client + document-store seam.
/// </summary>
public sealed record TodoItem : IDocument
{
    [JsonPropertyName("id")]
    public int TodoId { get; init; }

    [JsonPropertyName("userId")]
    public int UserId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("completed")]
    public bool Completed { get; init; }

    [JsonIgnore]
    public string Id => TodoId.ToString();
}
