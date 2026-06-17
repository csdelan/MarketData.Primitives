
namespace MarketData.Workers;

/// <summary>
/// Collection of <see cref="JobSchedule"/> entries bound from the <c>Schedules</c> configuration section.
/// </summary>
public sealed class ScheduleOptions
{
    public const string SectionName = "Schedules";

    public List<JobSchedule> Jobs { get; set; } = new();
}
