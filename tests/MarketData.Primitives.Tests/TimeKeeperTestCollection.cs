namespace MarketData.Primitives.Tests
{
    // Dedicated collection to avoid parallel tests mutating the global TimeKeeperProvider concurrently
    [CollectionDefinition("TimeKeeper", DisableParallelization = true)]
    public class TimeKeeperCollection : ICollectionFixture<TimeKeeperFixture>
    {
    }

    public class TimeKeeperFixture : IDisposable
    {
        public TimeKeeperFixture()
        {
            TimeKeeperProvider.SetRealTimeKeeper();
        }

        public void Dispose()
        {
            TimeKeeperProvider.SetRealTimeKeeper();
        }
    }
}
