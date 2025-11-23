namespace MarketData.Primitives
{
    public class SimulatedTimeKeeper : ITimeKeeper
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
