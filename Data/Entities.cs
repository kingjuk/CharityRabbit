namespace CharityRabbit.Data;

// Relational schema replacing the neo4j graph. Neo4j internal id() -> bigint identity PKs;
// User keyed by OIDC subject (the whole app joins on it). Single-valued edges (category,
// subcategory, contact, location, posting org) become columns/FKs; true many-to-many edges
// become join tables (some carry edge properties — see the *At/Role/JoinedDate fields).

public class User
{
    public string UserId { get; set; } = "";   // OIDC subject
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }          // read by members/participants, never written today
}

public class Contact                            // MERGE (c:Contact {email})
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string? Name { get; set; }
    public string? Phone { get; set; }
}

public class Location                           // MERGE (l:Location {city,state,country,zip})
{
    public long Id { get; set; }
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Country { get; set; } = "";
    public string Zip { get; set; } = "";
}

public class Skill
{
    public long Id { get; set; }
    public string Name { get; set; } = "";      // original casing; deduped case-insensitively
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
}

public class Tag { public long Id { get; set; } public string Name { get; set; } = ""; }

public class Organization
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Mission { get; set; }
    public string? Vision { get; set; }
    public string ContactEmail { get; set; } = "";
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? OrganizationType { get; set; }
    public string? TaxId { get; set; }
    public DateTimeOffset? FoundedDate { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public List<string> FocusAreas { get; set; } = new();   // text[]
    public List<string> Tags { get; set; } = new();         // text[] (org tags are raw, NOT Tag nodes)
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastModifiedDate { get; set; }
    public string Status { get; set; } = "Active";          // Active / Inactive(soft delete) / Pending
    public bool IsVerified { get; set; }
}

public class GoodWork
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? DetailedDescription { get; set; }
    public string? Category { get; set; }           // was BELONGS_TO edge
    public string? SubCategory { get; set; }         // was HAS_SUBCATEGORY edge
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public double? EstimatedDurationMinutes { get; set; }   // neo4j stored TimeSpan.TotalMinutes
    public string? EffortLevel { get; set; } = "Moderate";
    public bool IsAccessible { get; set; }
    public bool IsVirtual { get; set; }
    public int? MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public int? MinimumAge { get; set; }
    public bool FamilyFriendly { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationWebsite { get; set; }
    public bool ParkingAvailable { get; set; }
    public bool PublicTransitAccessible { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? ImpactDescription { get; set; }
    public int? EstimatedPeopleHelped { get; set; }
    public string Status { get; set; } = "Active";
    public bool OutdoorActivity { get; set; }
    public bool WeatherDependent { get; set; }
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }           // ownership key (g.createdBy == userId)

    public long? ContactId { get; set; }  public Contact? Contact { get; set; }        // HAS_CONTACT
    public long? LocationId { get; set; } public Location? Location { get; set; }       // LOCATED_IN
    public long? OrganizationId { get; set; } public Organization? Organization { get; set; } // POSTED_BY

    public List<Tag> Tags { get; set; } = new();                 // TAGGED_WITH
    public List<Skill> Skills { get; set; } = new();             // REQUIRES_SKILL
}

// ---- Join tables (many-to-many). Edge-property carriers are explicit entities. ----

public class UserSkill { public string UserId { get; set; } = ""; public long SkillId { get; set; } }

public class UserInterested   // INTERESTED_IN (+ timestamp)
{
    public string UserId { get; set; } = "";
    public long GoodWorkId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public GoodWork GoodWork { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class UserSignup        // SIGNED_UP_FOR (+ timestamp; drives current_participants)
{
    public string UserId { get; set; } = "";
    public long GoodWorkId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public GoodWork GoodWork { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class OrganizationMember     // MEMBER_OF (+ role, joinedDate)
{
    public string UserId { get; set; } = "";
    public long OrganizationId { get; set; }
    public string Role { get; set; } = "Member";
    public DateTimeOffset JoinedDate { get; set; } = DateTimeOffset.UtcNow;
    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}

public class OrganizationAdmin      // ADMIN_OF (+ since)
{
    public string UserId { get; set; } = "";
    public long OrganizationId { get; set; }
    public DateTimeOffset Since { get; set; } = DateTimeOffset.UtcNow;
    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}

public class UserFriend             // FRIENDS_WITH (unpopulated today; kept for parity)
{
    public string UserId { get; set; } = "";
    public string FriendUserId { get; set; } = "";
}
