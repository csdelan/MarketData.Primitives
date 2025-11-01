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

        private class RealTimeKeeper : ITimeKeeper
        {
            public RealTimeKeeper() { }
            public DateTimeOffset Now => DateTimeOffset.Now;
            public Task SetTime(DateTimeOffset time) => WaitTime(time);
            public Task WaitTime(DateTimeOffset time)
            {
                if (time <= Now) return Task.CompletedTask;
                return Task.Delay(time - Now);
            }
        }

        private class SimulatedTimeKeeper : ITimeKeeper
        {
            private DateTimeOffset _currentTime;
            public SimulatedTimeKeeper(DateTimeOffset initialTime)
            {
                _currentTime = initialTime;
            }
            public DateTimeOffset Now => _currentTime;
            public Task SetTime(DateTimeOffset time)
            {
                _currentTime = time;
                return Task.CompletedTask;
            }
            public Task WaitTime(DateTimeOffset time)
            {
                if (time <= _currentTime) return Task.CompletedTask;
                _currentTime = time;
                return Task.CompletedTask;
            }
        }
    }
}
