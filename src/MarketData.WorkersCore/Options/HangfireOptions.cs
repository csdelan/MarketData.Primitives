using System.ComponentModel.DataAnnotations;

namespace MarketData.Workers;

/// <summary>
/// Hangfire storage options. Bound from the <c>Hangfire</c> configuration section.
/// <para>
/// The default <see cref="DbPath"/> places the SQLite file under <c>%ProgramData%\MarketData\</c>
/// so every worker process on the same machine shares the same Hangfire store and any one of them
/// can host the consolidated dashboard.
/// </para>
/// </summary>
public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";

    /// <summary>
    /// Absolute path to the shared SQLite database file.  The parent directory is created
    /// automatically on startup if it does not already exist.
    /// </summary>
    [Required]
    public string DbPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "MarketData", "hangfire.db");
}
