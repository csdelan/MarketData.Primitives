namespace MarketData.Primitives
{
    public static class RthEstimator
    {
        private const int NyseRthSeconds = 6 * 60 * 60 + 30 * 60; // 23400

        public static int EstimateBarsPerDay(Resolution res)
        {
            if (!res.IsIntraday()) return 1;
            var seconds = res.ToIntervalSeconds();
            if (seconds <= 0) return 1;
            return Math.Max(1, NyseRthSeconds / seconds);
        }
    }
}
