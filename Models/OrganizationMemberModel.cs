namespace CharityRabbit.Models;

public class OrganizationMemberModel
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "Member"; // Admin, Member, Volunteer
    public DateTime JoinedDate { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int ContributedEvents { get; set; }
}
