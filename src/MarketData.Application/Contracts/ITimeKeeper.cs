namespace MarketData.Application.Contracts;

public interface ITimeKeeper
{
    DateTimeOffset Now { get; }
    Task SetTime(DateTimeOffset time);
    Task WaitTime(DateTimeOffset time);
}
