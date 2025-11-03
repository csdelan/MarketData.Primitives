using Core;

namespace MarketData.Primitives
{
    /// <summary>
    /// Represents a timeframe for candlesticks.  "1d, 5m, etc"
    /// </summary>
    public struct Resolution
    {
        public ResolutionUnit Unit { get; init; }
        public uint Count { get; init; }

        /// <summary>
        /// Default value for resolution
        /// </summary>
        public Resolution()
        {
            Unit = ResolutionUnit.Minutes;
            Count = 1;
        }

        public Resolution(uint count, ResolutionUnit unit)
        {
            Unit = unit;
            Count = count;
        }

        /// <summary>
        /// Represents an empty or uninitialized resolution, with zero count and Minutes as the unit.
        /// </summary>
        public static readonly Resolution Empty = new Resolution(0, ResolutionUnit.Minutes);

        /// <summary>
        /// True when this resolution has no duration (Count == 0).
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// Intraday refers to less than 1 day proper
        /// </summary>
        public bool IsIntraday
        {
            get
            {
                if ((Unit == ResolutionUnit.Days) || (Unit == ResolutionUnit.Weeks) || (Unit == ResolutionUnit.Months) ||
                    (Unit == ResolutionUnit.Years) || (Unit == ResolutionUnit.Quarters))
                    return false;
                if ((Unit == ResolutionUnit.Hours) && Count >= 24) return false;
                if ((Unit == ResolutionUnit.Minutes) && Count >= 3660) return false;
                return true;
            }
        }

        public TimeSpan GetDuration()
        {
            if (Count == 0)
                return TimeSpan.Zero;
            return Unit switch
            {
                ResolutionUnit.Seconds => TimeSpan.FromSeconds(Count),
                ResolutionUnit.Minutes => TimeSpan.FromMinutes(Count),
                ResolutionUnit.Hours => TimeSpan.FromHours(Count),
                ResolutionUnit.Days => TimeSpan.FromDays(Count),
                ResolutionUnit.Weeks => TimeSpan.FromDays(Count * 7),
                ResolutionUnit.Months => throw new InvalidOperationException("Cannot represent variable-length units (Months) as a fixed TimeSpan."),
                ResolutionUnit.Quarters => throw new InvalidOperationException("Cannot represent variable-length units (Quarters) as a fixed TimeSpan."),
                ResolutionUnit.Years => throw new InvalidOperationException("Cannot represent variable-length units (Years) as a fixed TimeSpan."),
                _ => throw new InvalidOperationException("Invalid resolution unit.")
            };
        }

        /// <summary>
        /// Calculates the exact duration of the next resolution event starting from the specified time.
        /// For variable-length units (months, quarters, years), the duration depends on the start time.
        /// </summary>
        /// <param name="start">The reference start time.</param>
        /// <returns>The exact duration as a TimeSpan.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the resolution unit is invalid.</exception>
        public TimeSpan GetDurationToNextResolutionEvent(DateTimeOffset start)
        {
            if (Count == 0)
                return TimeSpan.Zero;

            DateTimeOffset next = GetNextEvent(start);
            return next - start;
        }

        public TimeSpan GetExactDuration()
        {
            return GetDurationToNextResolutionEvent(TimeKeeperProvider.Now);
        }

        public TimeSpan GetTimeSpan()
        {
            if (Count == 0)
                return TimeSpan.Zero;

            return Unit switch
            {
                ResolutionUnit.Seconds => TimeSpan.FromSeconds(Count),
                ResolutionUnit.Minutes => TimeSpan.FromMinutes(Count),
                ResolutionUnit.Hours => TimeSpan.FromHours(Count),
                ResolutionUnit.Days => TimeSpan.FromDays(Count),
                ResolutionUnit.Weeks => TimeSpan.FromDays(Count * 7),

                // Variable-length units cannot be represented as a fixed TimeSpan
                ResolutionUnit.Months or ResolutionUnit.Quarters or ResolutionUnit.Years =>
                    throw new InvalidOperationException("Cannot represent variable-length units (Months, Quarters, Years) as a fixed TimeSpan."),
                _ => throw new InvalidOperationException("Invalid resolution unit.")
            };
        }

        /// <summary>
        /// Get last fired time event for the given resolution (floors to the most recent boundary at or before the given time).
        /// </summary>
        /// <param name="time">current time reference</param>
        /// <returns>The last boundary time at or before the given time</returns>
        public DateTimeOffset GetLastEvent(DateTimeOffset time)
        {
            if (Count == 0)
                throw new InvalidOperationException("Cannot calculate last event for zero-count resolution.");

            long ticksPerUnit = Unit switch
            {
                ResolutionUnit.Seconds => TimeSpan.TicksPerSecond,
                ResolutionUnit.Minutes => TimeSpan.TicksPerMinute,
                ResolutionUnit.Hours => TimeSpan.TicksPerHour,
                ResolutionUnit.Days => TimeSpan.TicksPerDay,
                ResolutionUnit.Weeks => TimeSpan.TicksPerDay * 7,
                _ => 0
            };

            if (ticksPerUnit > 0)
            {
                long blockTicks = (long)Count * ticksPerUnit;
                long ticks = time.Ticks - (time.Ticks % blockTicks);
                return new DateTimeOffset(ticks, time.Offset);
            }

            // Variable-length units handled via month indexing
            int monthsPerBlock = Unit switch
            {
                ResolutionUnit.Months => (int)Count,
                ResolutionUnit.Quarters => checked((int)Count * 3),
                ResolutionUnit.Years => checked((int)Count * 12),
                _ => throw new InvalidOperationException("Invalid resolution unit.")
            };

            int monthIndex = checked(time.Year * 12 + (time.Month - 1));
            int startIndex = monthIndex - (monthIndex % monthsPerBlock);
            int startYear = startIndex / 12;
            int startMonth = (startIndex % 12) + 1;
            return new DateTimeOffset(startYear, startMonth, 1, 0, 0, 0, time.Offset);
        }

        /// <summary>
        /// Gets the next occurrence of the resolution boundary after the specified time.
        /// </summary>
        /// <param name="time">The reference time.</param>
        /// <returns>The next boundary time.</returns>
        public DateTimeOffset GetNextEvent(DateTimeOffset time)
        {
            if (Count == 0)
                throw new InvalidOperationException("Cannot calculate next event for zero-count resolution.");

            long ticksPerUnit = Unit switch
            {
                ResolutionUnit.Seconds => TimeSpan.TicksPerSecond,
                ResolutionUnit.Minutes => TimeSpan.TicksPerMinute,
                ResolutionUnit.Hours => TimeSpan.TicksPerHour,
                ResolutionUnit.Days => TimeSpan.TicksPerDay,
                ResolutionUnit.Weeks => TimeSpan.TicksPerDay * 7,
                _ => 0
            };

            if (ticksPerUnit > 0)
            {
                long ticks = time.Ticks - (time.Ticks % ((long)Count * ticksPerUnit)) + (long)Count * ticksPerUnit;
                return new DateTimeOffset(ticks, time.Offset);
            }

            return Unit switch
            {
                ResolutionUnit.Months => time.AddMonths((int)Count).WithDay(1),
                ResolutionUnit.Quarters => GetNextQuarter(time, (int)Count),
                ResolutionUnit.Years => time.AddYears((int)Count).WithDayAndMonth(1, 1),
                _ => throw new InvalidOperationException("Invalid resolution unit.")
            };
        }

        /// <summary>
        /// Gets the next quarter boundary after the specified date, or the start of the current
        /// quarter if count = 0.
        /// </summary>
        /// <param name="date">start date</param>
        /// <param name="count"># of quarters forward to find</param>
        /// <returns>The DateTimeOffset representing the first day of the quarter found</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private DateTimeOffset GetNextQuarter(DateTimeOffset date, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative.");

            // Determine the current quarter (1-based)
            int currentQuarter = ((date.Month - 1) / 3) + 1;

            // If count == 0, return the start of the current quarter
            if (count == 0)
            {
                int startMonth = ((currentQuarter - 1) * 3) + 1;
                return new DateTimeOffset(date.Year, startMonth, 1, 0, 0, 0, date.Offset);
            }

            // Calculate the target quarter and year
            int totalQuarters = (currentQuarter - 1) + count;
            int targetYear = date.Year + (totalQuarters / 4);
            int targetQuarter = (totalQuarters % 4) + 1;
            int targetMonth = ((targetQuarter - 1) * 3) + 1;

            return new DateTimeOffset(targetYear, targetMonth, 1, 0, 0, 0, date.Offset);
        }

        /// <summary>
        /// Gets the DateTimeOffset representing the start of the next Resolution boundary
        /// </summary>
        /// <exception cref="NotSupportedException">The ResolutionUnit must be a valid value</exception>
        public DateTimeOffset GetNextEvent()
        {
            return GetNextEvent(TimeKeeperProvider.Now);
        }

        public override string ToString() => ToShorthand();

        public string ToShorthand()
        {
            switch (Unit)
            {
                case ResolutionUnit.Seconds: return $"{Count}s";
                case ResolutionUnit.Minutes: return $"{Count}m";
                case ResolutionUnit.Hours: return $"{Count}h";
                case ResolutionUnit.Days: return $"{Count}d";
                case ResolutionUnit.Weeks: return $"{Count}w";
                case ResolutionUnit.Months: return $"{Count}M";
                case ResolutionUnit.Quarters: return $"{Count}Q";
                case ResolutionUnit.Years: return $"{Count}Y";
                default: throw new InvalidDataException("Invalid resolution");
            }
        }

        public enum StandardResolution
        {
            OneMinute = 1,
            TwoMinutes = 2,
            FiveMinutes = 5,
            FifteenMinutes = 15,
            ThirtyMinutes = 30,
            OneHour = 60,
            TwoHours = 120,
            FourHours = 240,
            OneDay = 1440, // 24 hours
            TwoDays = 2880, // 48 hours
            OneWeek = 10080, // 7 days
            TwoWeeks = 20160, // 14 days
            OneMonth = 43200, // Approx. 30 days
            OneQuarter = 129600, // Approx. 90 days
        }

        public readonly Dictionary<StandardResolution, Resolution> GetStandardResolutions()
        {
            return new Dictionary<StandardResolution, Resolution>
            {
                { StandardResolution.OneMinute, new Resolution(1, ResolutionUnit.Minutes) },
                { StandardResolution.TwoMinutes, new Resolution(2, ResolutionUnit.Minutes) },
                { StandardResolution.FiveMinutes, new Resolution(5, ResolutionUnit.Minutes) },
                { StandardResolution.FifteenMinutes, new Resolution(15, ResolutionUnit.Minutes) },
                { StandardResolution.ThirtyMinutes, new Resolution(30, ResolutionUnit.Minutes) },
                { StandardResolution.OneHour, new Resolution(1, ResolutionUnit.Hours) },
                { StandardResolution.TwoHours, new Resolution(2, ResolutionUnit.Hours) },
                { StandardResolution.OneDay, new Resolution(1, ResolutionUnit.Days) },
                { StandardResolution.TwoDays, new Resolution(2, ResolutionUnit.Days) },
                { StandardResolution.OneWeek, new Resolution(1, ResolutionUnit.Weeks) },
                { StandardResolution.TwoWeeks, new Resolution(2, ResolutionUnit.Weeks) },
                { StandardResolution.OneMonth, new Resolution(1, ResolutionUnit.Months) },
                { StandardResolution.OneQuarter, new Resolution(1, ResolutionUnit.Quarters) }
            };
        }

        public Resolution GetResolution(StandardResolution res)
        {
            var resolutions = GetStandardResolutions();
            if (resolutions.TryGetValue(res, out var resolution))
            {
                return resolution;
            }
            else
            {
                throw new ArgumentException($"Invalid standard resolution: {res}", nameof(StandardResolution));
            }
        }
    }
}
