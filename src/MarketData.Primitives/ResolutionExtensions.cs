namespace MarketData.Primitives
{
    public static class ResolutionExtensions
    {
        public static bool IsIntraday(this Resolution res) =>
            res.Unit == ResolutionUnit.Seconds || res.Unit == ResolutionUnit.Minutes || res.Unit == ResolutionUnit.Hours;

        public static int ToIntervalSeconds(this Resolution res) => res.Unit switch
        {
            ResolutionUnit.Seconds => (int)res.Count,
            ResolutionUnit.Minutes => (int)res.Count * 60,
            ResolutionUnit.Hours => (int)res.Count * 3600,
            _ => throw new NotSupportedException($"Unsupported intraday unit: {res.Unit}")
        };
    }
}
