using System.Text.Json;
using MarketData.Application.Contracts;

namespace MarketData.Application.Calendar;

/// <summary>
/// Per-year holiday/early-close overrides loaded from JSON. These take precedence over
/// computed rules and the bundled special-closure table — the pressure valve for
/// forward-looking, ad-hoc corrections.
/// </summary>
public sealed record HolidayOverride(
    IReadOnlyList<Holiday> Holidays,
    IReadOnlyList<Holiday> EarlyCloses);

/// <summary>
/// Loads <see cref="HolidayOverride"/> files from
/// <c>{configPath}/holidays/holidays-{year}.json</c>.
/// <para>
/// New schema: <c>{ "holidays": [{ "date": "2025-01-01", "name": "..." }], "earlyCloses": [...] }</c>.
/// A legacy schema with bare ISO-date string arrays (<c>{ "holidays": ["2025-01-01"], "halfDays": [...] }</c>)
/// is also accepted; names are synthesized for those entries.
/// </para>
/// </summary>
public sealed class HolidayOverrideLoader
{
    private readonly string _configPath;

    public HolidayOverrideLoader(string? configPath = null)
    {
        _configPath = configPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "OneDrive", "TradingSystem", "config");
    }

    /// <summary>Loads overrides for a year, or null if no file exists or it cannot be parsed.</summary>
    public HolidayOverride? Load(int year)
    {
        try
        {
            string file = Path.Combine(_configPath, "holidays", $"holidays-{year}.json");
            if (!File.Exists(file))
                return null;

            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;

            var holidays = ReadEntries(root, "holidays", isEarlyClose: false, "Holiday");
            // Accept both the new "earlyCloses" and the legacy "halfDays".
            var earlyCloses = ReadEntries(root, "earlyCloses", isEarlyClose: true, "Early Close");
            if (earlyCloses.Count == 0)
                earlyCloses = ReadEntries(root, "halfDays", isEarlyClose: true, "Early Close");

            return new HolidayOverride(holidays, earlyCloses);
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<Holiday> ReadEntries(
        JsonElement root, string propertyName, bool isEarlyClose, string defaultName)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<Holiday>();
        foreach (var element in array.EnumerateArray())
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    // Legacy bare-date form.
                    if (DateOnly.TryParse(element.GetString(), out var bareDate))
                        result.Add(new Holiday(bareDate, defaultName, isEarlyClose));
                    break;

                case JsonValueKind.Object:
                    if (element.TryGetProperty("date", out var dateProp) &&
                        DateOnly.TryParse(dateProp.GetString(), out var date))
                    {
                        string name = element.TryGetProperty("name", out var nameProp)
                            ? nameProp.GetString() ?? defaultName
                            : defaultName;
                        result.Add(new Holiday(date, name, isEarlyClose));
                    }
                    break;
            }
        }

        return result;
    }
}
