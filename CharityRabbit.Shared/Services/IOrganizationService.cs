using CharityRabbit.Models;

namespace CharityRabbit.Data;

/// <summary>
/// Organization data access shared by the server (EF Core) and mobile (HTTP) implementations.
/// <c>userId</c> parameters are the authenticated subject on the server; HTTP implementations
/// ignore them (identity comes from the bearer token), except where a parameter designates a
/// target member (documented per member).
/// </summary>
public interface IOrganizationService
{
    Task<bool> IsSlugAvailableAsync(string slug, long? excludeOrgId = null);
    Task<OrganizationModel> CreateOrganizationAsync(OrganizationModel organization, string userId);
    Task<OrganizationModel?> GetOrganizationBySlugAsync(string slug, string? userId = null);
    Task<List<OrganizationModel>> GetOrganizationsAsync(int skip = 0, int limit = 20, string? searchTerm = null);
    Task<int> CountOrganizationsAsync(string? searchTerm = null);
    Task<List<OrganizationModel>> GetUserOrganizationsAsync(string userId);
    /// <param name="userId">The member being added — may legitimately differ from the caller
    /// (admin adding a member). The API authorizes the caller separately.</param>
    Task<bool> AddMemberAsync(long organizationId, string userId, string role = "Member");
    /// <param name="userId">The member being removed (self-leave or admin removal).</param>
    Task<bool> RemoveMemberAsync(long organizationId, string userId);
    Task<List<OrganizationMemberModel>> GetOrganizationMembersAsync(long organizationId);
    Task<bool> UpdateOrganizationAsync(OrganizationModel organization);
    Task<bool> DeleteOrganizationAsync(long organizationId);
    Task<bool> IsUserAdminAsync(long organizationId, string userId);
    /// <param name="userId">The member being promoted.</param>
    Task<bool> PromoteToAdminAsync(long organizationId, string userId);
}
