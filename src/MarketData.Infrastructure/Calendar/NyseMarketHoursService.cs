using MarketData.Application.Contracts;
using MarketData.Primitives;

namespace MarketData.Infrastructure.Calendar;

public sealed class NyseMarketHoursService(ITimeKeeper timeKeeper, IMarketCalendarService calendarService) : IMarketHoursService
{
    public async Task<bool> IsOpenAsync(string venue, DateTimeOffset asOfUtc, CancellationToken ct = default)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(asOfUtc, MarketHours.EasternTimeZone);
        DateOnly date = DateOnly.FromDateTime(local.DateTime);

        if (!await calendarService.IsTradingDayAsync(venue, date, ct))
            return false;

        var session = await calendarService.GetSessionAsync(venue, date, ct);
        if (session is null)
            return false;

        var localTime = TimeOnly.FromDateTime(local.DateTime);
        return localTime >= session.Open && localTime < session.Close;
    }

    public async Task<MarketHoursStatus> GetCurrentStatusAsync(string venue, CancellationToken ct = default)
    {
        DateTimeOffset asOfUtc = timeKeeper.Now;
        DateTimeOffset asOfLocal = TimeZoneInfo.ConvertTime(asOfUtc, MarketHours.EasternTimeZone);
        DateOnly date = DateOnly.FromDateTime(asOfLocal.DateTime);

        bool isTradingDay = await calendarService.IsTradingDayAsync(venue, date, ct);
        var session = isTradingDay
            ? await calendarService.GetSessionAsync(venue, date, ct)
            : null;

        var localTime = TimeOnly.FromDateTime(asOfLocal.DateTime);
        bool isOpen = session is not null && localTime >= session.Open && localTime < session.Close;

        return new MarketHoursStatus(isTradingDay, isOpen, asOfLocal, session);
    }
}
