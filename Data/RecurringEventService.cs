using CharityRabbit.Models;

namespace CharityRabbit.Data;

public class RecurringEventService
{
    /// <summary>
    /// Generates recurring event instances based on the recurrence pattern
    /// </summary>
    public List<GoodWorksModel> GenerateRecurringInstances(GoodWorksModel masterEvent, int maxInstances = 52)
    {
        if (!masterEvent.IsRecurring || string.IsNullOrEmpty(masterEvent.RecurrencePattern))
        {
            return new List<GoodWorksModel> { masterEvent };
        }

        var instances = new List<GoodWorksModel>();
        var pattern = ParseRecurrencePattern(masterEvent.RecurrencePattern);
        
        if (!masterEvent.StartTime.HasValue)
        {
            return new List<GoodWorksModel> { masterEvent };
        }

        var currentDate = masterEvent.StartTime.Value;
        var endDate = masterEvent.RecurrenceEndDate ?? currentDate.AddYears(1); // Default to 1 year
        var count = 0;

        while (currentDate <= endDate && count < maxInstances)
        {
            var instance = CloneEvent(masterEvent);
            instance.StartTime = currentDate;
            
            if (masterEvent.EndTime.HasValue)
            {
                var duration = masterEvent.EndTime.Value - masterEvent.StartTime.Value;
                instance.EndTime = currentDate.Add(duration);
            }

            instances.Add(instance);
            
            currentDate = GetNextOccurrence(currentDate, pattern);
            count++;
        }

        return instances;
    }

    /// <summary>
    /// Gets upcoming occurrences for display purposes
    /// </summary>
    public List<DateTime> GetUpcomingOccurrences(GoodWorksModel recurringEvent, int count = 5)
    {
        if (!recurringEvent.IsRecurring || !recurringEvent.StartTime.HasValue)
        {
            return new List<DateTime>();
        }

        var occurrences = new List<DateTime>();
        var pattern = ParseRecurrencePattern(recurringEvent.RecurrencePattern ?? "");
        var currentDate = recurringEvent.StartTime.Value;
        
        // Start from today if the original start date is in the past
        if (currentDate < DateTime.Now)
        {
            currentDate = DateTime.Now.Date.Add(currentDate.TimeOfDay);
        }

        var endDate = recurringEvent.RecurrenceEndDate ?? currentDate.AddYears(1);
        var iterations = 0;

        while (occurrences.Count < count && currentDate <= endDate && iterations < 100)
        {
            if (currentDate >= DateTime.Now)
            {
                occurrences.Add(currentDate);
            }
            currentDate = GetNextOccurrence(currentDate, pattern);
            iterations++;
        }

        return occurrences;
    }

    /// <summary>
    /// Formats recurrence pattern for display
    /// </summary>
    public string FormatRecurrencePattern(string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return "Does not repeat";
        }

        var parsed = ParseRecurrencePattern(pattern);
        
        return parsed.Type switch
        {
            RecurrenceType.Daily => $"Daily (every {parsed.Interval} day{(parsed.Interval > 1 ? "s" : "")})",
            RecurrenceType.Weekly => $"Weekly on {string.Join(", ", parsed.DaysOfWeek ?? new List<DayOfWeek>())}",
            RecurrenceType.Monthly => $"Monthly on day {parsed.DayOfMonth}",
            RecurrenceType.Yearly => $"Yearly on {parsed.MonthOfYear}/{parsed.DayOfMonth}",
            _ => pattern
        };
    }

    private RecurrencePatternData ParseRecurrencePattern(string pattern)
    {
        // Format: "DAILY:1" or "WEEKLY:1:MON,WED,FRI" or "MONTHLY:1:15" or "YEARLY:1:3:15"
        var parts = pattern.Split(':');
        
        if (parts.Length == 0)
        {
            return new RecurrencePatternData { Type = RecurrenceType.None };
        }

        var type = parts[0].ToUpper() switch
        {
            "DAILY" => RecurrenceType.Daily,
            "WEEKLY" => RecurrenceType.Weekly,
            "MONTHLY" => RecurrenceType.Monthly,
            "YEARLY" => RecurrenceType.Yearly,
            _ => RecurrenceType.None
        };

        var interval = parts.Length > 1 && int.TryParse(parts[1], out var i) ? i : 1;

        var data = new RecurrencePatternData
        {
            Type = type,
            Interval = interval
        };

        // Parse additional parameters based on type
        if (type == RecurrenceType.Weekly && parts.Length > 2)
        {
            data.DaysOfWeek = parts[2].Split(',')
                .Select(d => Enum.TryParse<DayOfWeek>(d, true, out var day) ? day : (DayOfWeek?)null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();
        }
        else if (type == RecurrenceType.Monthly && parts.Length > 2)
        {
            data.DayOfMonth = int.TryParse(parts[2], out var dom) ? dom : 1;
        }
        else if (type == RecurrenceType.Yearly && parts.Length > 3)
        {
            data.MonthOfYear = int.TryParse(parts[2], out var moy) ? moy : 1;
            data.DayOfMonth = int.TryParse(parts[3], out var dom) ? dom : 1;
        }

        return data;
    }

    private DateTime GetNextOccurrence(DateTime current, RecurrencePatternData pattern)
    {
        return pattern.Type switch
        {
            RecurrenceType.Daily => current.AddDays(pattern.Interval),
            RecurrenceType.Weekly => GetNextWeeklyOccurrence(current, pattern),
            RecurrenceType.Monthly => current.AddMonths(pattern.Interval),
            RecurrenceType.Yearly => current.AddYears(pattern.Interval),
            _ => current.AddDays(7) // Default to weekly
        };
    }

    private DateTime GetNextWeeklyOccurrence(DateTime current, RecurrencePatternData pattern)
    {
        if (pattern.DaysOfWeek == null || !pattern.DaysOfWeek.Any())
        {
            return current.AddDays(7 * pattern.Interval);
        }

        var nextDay = current.AddDays(1);
        var daysChecked = 0;
        var maxDaysToCheck = 7 * pattern.Interval + 7; // Check up to interval weeks + 1 week

        while (daysChecked < maxDaysToCheck)
        {
            if (pattern.DaysOfWeek.Contains(nextDay.DayOfWeek))
            {
                return nextDay;
            }
            nextDay = nextDay.AddDays(1);
            daysChecked++;
        }

        return current.AddDays(7 * pattern.Interval);
    }

    private GoodWorksModel CloneEvent(GoodWorksModel source)
    {
        return new GoodWorksModel
        {
            Name = source.Name,
            Category = source.Category,
            SubCategory = source.SubCategory,
            Description = source.Description,
            DetailedDescription = source.DetailedDescription,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            Address = source.Address,
            EffortLevel = source.EffortLevel,
            IsAccessible = source.IsAccessible,
            IsVirtual = source.IsVirtual,
            MaxParticipants = source.MaxParticipants,
            MinimumAge = source.MinimumAge,
            FamilyFriendly = source.FamilyFriendly,
            OrganizationName = source.OrganizationName,
            OrganizationWebsite = source.OrganizationWebsite,
            ParkingAvailable = source.ParkingAvailable,
            PublicTransitAccessible = source.PublicTransitAccessible,
            SpecialInstructions = source.SpecialInstructions,
            ImpactDescription = source.ImpactDescription,
            EstimatedPeopleHelped = source.EstimatedPeopleHelped,
            ContactName = source.ContactName,
            ContactEmail = source.ContactEmail,
            ContactPhone = source.ContactPhone,
            RequiredSkills = new List<string>(source.RequiredSkills),
            Tags = new List<string>(source.Tags),
            WhatToBring = new List<string>(source.WhatToBring),
            OutdoorActivity = source.OutdoorActivity,
            WeatherDependent = source.WeatherDependent,
            EstimatedDuration = source.EstimatedDuration,
            IsRecurring = true, // Mark as part of recurring series
            RecurrencePattern = source.RecurrencePattern
        };
    }

    private enum RecurrenceType
    {
        None,
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    private class RecurrencePatternData
    {
        public RecurrenceType Type { get; set; }
        public int Interval { get; set; } = 1;
        public List<DayOfWeek>? DaysOfWeek { get; set; }
        public int DayOfMonth { get; set; } = 1;
        public int MonthOfYear { get; set; } = 1;
    }
}
