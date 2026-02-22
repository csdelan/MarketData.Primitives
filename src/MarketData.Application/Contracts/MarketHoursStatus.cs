namespace MarketData.Application.Contracts;

public sealed record MarketHoursStatus(
    bool IsTradingDay,
    bool IsOpen,
    DateTimeOffset AsOfLocal,
    MarketSession? Session);
