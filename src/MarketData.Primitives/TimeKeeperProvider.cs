namespace MarketData.Primitives
{
    /// <summary>
    /// Manages the active time provider for the application.
    /// </summary>
    public static class TimeKeeperProvider
    {
        private static ITimeKeeper _currentTimeKeeper = new RealTimeKeeper();

        /// <summary>
        /// Gets the current time keeper instance (real-time or simulated).
        /// </summary>
        public static ITimeKeeper Current => _currentTimeKeeper;

        /// <summary>
        /// Gets the current time from the active provider.
        /// </summary>
        public static DateTimeOffset Now => Current.Now;

        /// <summary>
        /// Sets the real-time keeper for production use.
        /// </summary>
        public static void SetRealTimeKeeper()
        {
            _currentTimeKeeper = new RealTimeKeeper();
        }

        /// <summary>
        /// Sets a simulated time keeper for testing or backtesting.
        /// </summary>
        /// <param name="initialTime">The initial time for simulation.</param>
        public static void SetSimulatedTimeKeeper(DateTimeOffset initialTime)
        {
            _currentTimeKeeper = new SimulatedTimeKeeper(initialTime);
        }
    }
}
