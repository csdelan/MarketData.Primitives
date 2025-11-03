namespace MarketData.Primitives
{
    public static class MarketHours
    {
        public static readonly TimeSpan NyseOpen = new(9, 30, 0);
        public static readonly TimeSpan NyseClose = new(16, 0, 0);

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

        public static bool IsWithinSession(DateTimeOffset startUtc, TimeSpan duration, TimeSpan sessionOpen, TimeSpan sessionClose, TimeZoneInfo tz)
        {
            var start = TimeZoneInfo.ConvertTime(startUtc, tz);
            var end = TimeZoneInfo.ConvertTime(startUtc + duration, tz);
            return start.TimeOfDay >= sessionOpen && end.TimeOfDay <= sessionClose;
        }
    }
}
