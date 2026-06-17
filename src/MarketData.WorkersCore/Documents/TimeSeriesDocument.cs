using System.Text;
using Core;

namespace MarketData.Workers;

/// <summary>
/// Base for time-stamped documents persisted via <c>IDocumentStore&lt;T&gt;</c> (JSON file DB or
/// MongoDB). Derived types supply a stable, filesystem-safe <see cref="Id"/> (the JSON store uses it
/// as the file name) — typically a composite of <see cref="Timestamp"/> and a series key.
/// </summary>
public abstract record TimeSeriesDocument : IDocument
{
    /// <summary>Stable, filesystem-safe document key.</summary>
    public abstract string Id { get; }

    /// <summary>The instant this observation belongs to (UTC canonical).</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Collapses arbitrary text to a key segment safe for file names and Mongo ids.</summary>
    protected static string Slug(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.Trim())
            sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');

        return sb.ToString().Trim('-');
    }
}
