namespace CharityRabbit.Models;

public class ParticipantModel
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string RelationshipType { get; set; } = string.Empty; // "SIGNED_UP_FOR" or "INTERESTED_IN"
    public DateTime EngagementDate { get; set; }
}
