namespace CharityRabbit.Models;

public class OrganizationModel
{
    public long? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // URL-friendly identifier
    public string Description { get; set; } = string.Empty;
    public string? Mission { get; set; }
    public string? Vision { get; set; }
    
    // Contact Information
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }
    
    // Address
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Organization Details
    public string? OrganizationType { get; set; } // Nonprofit, Religious, Educational, etc.
    public string? TaxId { get; set; } // EIN or equivalent
    public DateTime? FoundedDate { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    
    // Social Media
    public string? FacebookUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    
    // Categories and Tags
    public List<string>? FocusAreas { get; set; } // Food, Education, Healthcare, etc.
    public List<string>? Tags { get; set; }
    
    // Admin and Members
    public string CreatedBy { get; set; } = string.Empty; // Admin user ID
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedDate { get; set; }
    
    // Statistics
    public int MemberCount { get; set; }
    public int EventCount { get; set; }
    public int VolunteerCount { get; set; } // Total volunteers who participated
    
    // Status
    public string Status { get; set; } = "Active"; // Active, Inactive, Pending
    public bool IsVerified { get; set; }
    
    // User-specific properties (not stored in DB)
    public bool IsUserAdmin { get; set; }
    public bool IsUserMember { get; set; }
}
