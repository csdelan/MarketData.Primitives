using MarketData.Application.Contracts;
using MarketData.Primitives;

namespace MarketData.Infrastructure.TimeKeeping;

public sealed class RealTimeTimeKeeper : ITimeKeeper
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;

    public Task SetTime(DateTimeOffset time)
    {
        throw new InvalidOperationException("RealTimeTimeKeeper cannot set time.");
    }

    public Task WaitTime(DateTimeOffset time)
    {
        TimeSpan delay = time - Now;
        if (delay <= TimeSpan.Zero)
            return Task.CompletedTask;

        return Task.Delay(delay);
    }
}
