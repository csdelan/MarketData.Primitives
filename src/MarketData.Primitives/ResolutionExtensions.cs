namespace MarketData.Primitives
{
    public static class ResolutionExtensions
    {
        public static bool IsIntraday(this Resolution res) =>
            res.Unit == ResolutionUnit.Seconds || res.Unit == ResolutionUnit.Minutes || res.Unit == ResolutionUnit.Hours;

        /// <summary>
        /// Converts an intraday resolution to interval seconds.
        /// Only valid for Seconds, Minutes, and Hours units.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when resolution is not intraday</exception>
        public static int ToIntervalSeconds(this Resolution res)
        {
            if (!res.IsIntraday())
                throw new InvalidOperationException($"ToIntervalSeconds() is only valid for intraday resolutions. Resolution '{res}' is not intraday.");

            return res.Unit switch
            {
                ResolutionUnit.Seconds => (int)res.Count,
                ResolutionUnit.Minutes => (int)res.Count * 60,
                ResolutionUnit.Hours => (int)res.Count * 3600,
                _ => throw new InvalidOperationException($"Unsupported intraday unit: {res.Unit}")
            };
        }
    }
}
