namespace MarketData.Infrastructure.Calendar;

internal static class MarketTimeZoneProvider
{
    public static TimeZoneInfo EasternTimeZone
    {
        get
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
                catch { return TimeZoneInfo.Local; }
            }
        }
    }
}
