using System.ComponentModel.DataAnnotations;

namespace CharityRabbit.Models
{
    public class GoodWorksModel
    {
        // Unique identifier
        public long? Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; } = string.Empty;

        // Add subcategory for more granular filtering
        public string? SubCategory { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        // Add detailed description for the details page
        public string? DetailedDescription { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }

        public string? Address { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public TimeSpan? EstimatedDuration { get; set; }

        [Required(ErrorMessage = "Contact Name is required.")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string ContactEmail { get; set; } = string.Empty;

        [Phone]
        public string ContactPhone { get; set; } = string.Empty;

        public string EffortLevel { get; set; } = "Moderate";

        public bool IsAccessible { get; set; } = false;
        public bool IsVirtual { get; set; } = false;

        // New properties for better discovery and engagement

        // Capacity management
        public int? MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; } = 0;

        // Skills and requirements
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> Tags { get; set; } = new();

        // Age restrictions
        public int? MinimumAge { get; set; }
        public bool FamilyFriendly { get; set; } = false;

        // Recurring event support
        public bool IsRecurring { get; set; } = false;
        public string? RecurrencePattern { get; set; } // "Weekly", "Monthly", etc.
        public DateTime? RecurrenceEndDate { get; set; }

        // Organization/charity details
        public string? OrganizationName { get; set; }
        public string? OrganizationWebsite { get; set; }

        // Additional logistics
        public bool ParkingAvailable { get; set; } = false;
        public bool PublicTransitAccessible { get; set; } = false;
        public string? SpecialInstructions { get; set; }
        public List<string> WhatToBring { get; set; } = new();

        // Impact tracking
        public string? ImpactDescription { get; set; }
        public int? EstimatedPeopleHelped { get; set; }

        // Status
        public string Status { get; set; } = "Active"; // Active, Cancelled, Completed, Full

        // User engagement (populated from relationships)
        public int InterestedCount { get; set; } = 0;
        public int SignedUpCount { get; set; } = 0;
        public bool IsUserInterested { get; set; } = false;
        public bool IsUserSignedUp { get; set; } = false;

        // Weather considerations
        public bool OutdoorActivity { get; set; } = false;
        public bool WeatherDependent { get; set; } = false;

        // Created/Modified tracking
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedDate { get; set; }
    }

    // Supporting model for user engagement
    public class UserGoodWorkEngagement
    {
        public string UserId { get; set; } = string.Empty;
        public long GoodWorkId { get; set; }
        public bool IsInterested { get; set; }
        public bool IsSignedUp { get; set; }
        public DateTime EngagementDate { get; set; }
        public string? Notes { get; set; }
    }

    // Supporting model for search/filter
    public class GoodWorksSearchCriteria
    {
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public List<string>? Tags { get; set; }
        public double? CenterLatitude { get; set; }
        public double? CenterLongitude { get; set; }
        public double? RadiusMiles { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public string? EffortLevel { get; set; }
        public bool? IsVirtual { get; set; }
        public bool? IsAccessible { get; set; }
        public bool? FamilyFriendly { get; set; }
        public List<string>? RequiredSkills { get; set; }
        public bool? HasAvailableSpots { get; set; }
        public string? SearchText { get; set; }
    }
}
