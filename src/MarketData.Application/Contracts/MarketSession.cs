namespace MarketData.Application.Contracts;

public sealed record MarketSession(TimeOnly Open, TimeOnly Close, bool IsHalfDay = false);
