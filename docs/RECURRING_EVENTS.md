# Recurring Events Implementation Guide

## Overview
This implementation adds full support for recurring events in CharityRabbit, allowing organizers to create events that repeat on a schedule.

## Components

### 1. RecurringEventService (`Data/RecurringEventService.cs`)
Core service that handles all recurring event logic:

**Key Methods:**
- `GenerateRecurringInstances()` - Creates multiple event instances from a recurring pattern
- `GetUpcomingOccurrences()` - Returns next N occurrences for display
- `FormatRecurrencePattern()` - Converts pattern string to human-readable format

**Supported Patterns:**
- **Daily**: `DAILY:1` (every day), `DAILY:2` (every 2 days)
- **Weekly**: `WEEKLY:1:MON,WED,FRI` (every week on Mon/Wed/Fri)
- **Monthly**: `MONTHLY:1:15` (15th of every month)
- **Yearly**: `YEARLY:1:3:15` (March 15th every year)

### 2. UI Integration

#### AddGoodWork Page
- Toggle switch to enable recurring events
- Dropdown to select pattern type (Daily/Weekly/Monthly/Yearly)
- Interval field (repeat every N days/weeks/months/years)
- Day selector chips for weekly events
- End date picker (optional, defaults to 1 year)

#### GoodWorkDetails Page
- Shows "Recurring Event" badge
- Displays formatted recurrence pattern
- Lists upcoming occurrences (up to 5)
- Shows recurrence end date if set

#### Profile Page
- "Recurring" chip on created events
- Shows formatted pattern below event date

## Usage Examples

### Creating a Weekly Event
```csharp
var event = new GoodWorksModel
{
    Name = "Community Garden Cleanup",
    StartTime = new DateTime(2024, 1, 15, 9, 0, 0),
    IsRecurring = true,
    RecurrencePattern = "WEEKLY:1:TUE,THU", // Every Tuesday and Thursday
    RecurrenceEndDate = new DateTime(2024, 12, 31)
};
```

### Creating a Monthly Event
```csharp
var event = new GoodWorksModel
{
    Name = "Food Bank Distribution",
    StartTime = new DateTime(2024, 1, 15, 10, 0, 0),
    IsRecurring = true,
    RecurrencePattern = "MONTHLY:1:15", // 15th of every month
    RecurrenceEndDate = null // Repeats indefinitely
};
```

## Database Schema

The recurring event data is stored in the GoodWork node with these properties:
- `isRecurring` (Boolean)
- `recurrencePattern` (String) - encoded pattern
- `recurrenceEndDate` (DateTime) - optional end date

## Future Enhancements

### Potential Improvements:
1. **Instance Management**
   - Allow signing up for individual instances vs. entire series
   - Track attendance per occurrence
   - Cancel individual instances

2. **Advanced Patterns**
   - "First Monday of month"
   - "Last Friday of month"
   - "Every weekday"
   - Custom patterns (e.g., "2nd and 4th Tuesday")

3. **Recurring Event Master**
   - Create a separate RecurringEventSeries node
   - Generate individual GoodWork instances linked to master
   - Update all instances when master is edited

4. **Calendar Integration**
   - Export to iCal/Google Calendar
   - Show in calendar view
   - Send reminders before each occurrence

5. **Analytics**
   - Track participation across all occurrences
   - Show attendance trends
   - Identify popular time slots

## Technical Notes

### Pattern Format
The recurrence pattern is stored as a colon-separated string:
- Format: `TYPE:INTERVAL:OPTIONS`
- Examples:
  - `DAILY:1` - Every day
  - `WEEKLY:2:MON,WED,FRI` - Every 2 weeks on M/W/F
  - `MONTHLY:1:15` - Monthly on 15th
  - `YEARLY:1:6:15` - Yearly on June 15

### Performance Considerations
- The service generates instances on-the-fly for display
- Maximum of 52 instances generated per call (configurable)
- Upcoming occurrences limited to 10 by default
- Patterns are parsed once and cached in memory

### Validation
The UI enforces:
- End date must be after start date
- Weekly events require at least one day selected
- Interval must be >= 1
- Monthly day must be 1-31
- Yearly month must be 1-12

## Migration Path

For existing events that should be recurring:
1. Set `IsRecurring = true`
2. Build appropriate pattern string
3. Set `RecurrenceEndDate` if applicable
4. Update in Neo4j database

## Testing Checklist

- [ ] Create daily recurring event
- [ ] Create weekly recurring event with multiple days
- [ ] Create monthly recurring event
- [ ] Create yearly recurring event
- [ ] Verify upcoming occurrences display correctly
- [ ] Verify formatted pattern shows correctly
- [ ] Test with end date
- [ ] Test without end date (indefinite)
- [ ] Verify recurring badge shows on lists
- [ ] Test editing recurring events
- [ ] Verify sign-ups work for recurring events
