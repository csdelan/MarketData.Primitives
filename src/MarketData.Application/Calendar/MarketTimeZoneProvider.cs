using System.Collections.Concurrent;

namespace MarketData.Application.Calendar;

/// <summary>
/// Resolves a venue timezone id to a <see cref="TimeZoneInfo"/>, tolerating the
/// Windows vs IANA naming difference. Results are cached.
/// </summary>
internal static class MarketTimeZoneProvider
{
    private static readonly ConcurrentDictionary<string, TimeZoneInfo> Cache = new();

    /// <summary>The US Eastern timezone (NYSE / US-equity venue).</summary>
    public static TimeZoneInfo EasternTimeZone => For("America/New_York");

    /// <summary>
    /// Resolves a timezone id. Tries the id as given, then the Windows/IANA counterpart,
    /// then falls back to <see cref="TimeZoneInfo.Local"/>.
    /// </summary>
    public static TimeZoneInfo For(string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        return Cache.GetOrAdd(timeZoneId, Resolve);
    }

    private static TimeZoneInfo Resolve(string timeZoneId)
    {
        if (TryFind(timeZoneId, out var tz))
            return tz;

        // Bridge between IANA ("America/New_York") and Windows ("Eastern Standard Time").
        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId) &&
            TryFind(windowsId, out tz))
            return tz;

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaId) &&
            TryFind(ianaId, out tz))
            return tz;

        return TimeZoneInfo.Local;
    }

    private static bool TryFind(string? id, out TimeZoneInfo tz)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                return true;
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        tz = TimeZoneInfo.Local;
        return false;
    }
}
