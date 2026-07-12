using CharityRabbit.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CharityRabbit.Data;

// Organizations, members/admins, backed by PostgreSQL (EF Core). Public API unchanged.
public class OrganizationService(CharityDbContext db)
{
    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        return Regex.Replace(slug, @"-+", "-");
    }

    private static DateTimeOffset? ToDto(DateTime? v) =>
        v.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : null;
    private static DateTime? FromDto(DateTimeOffset? v) => v?.UtcDateTime;

    public async Task<bool> IsSlugAvailableAsync(string slug, long? excludeOrgId = null) =>
        !await db.Organizations.AnyAsync(o => o.Slug == slug && (excludeOrgId == null || o.Id != excludeOrgId));

    public async Task<OrganizationModel> CreateOrganizationAsync(OrganizationModel organization, string userId)
    {
        if (string.IsNullOrEmpty(organization.Slug))
        {
            organization.Slug = GenerateSlug(organization.Name);
            var baseSlug = organization.Slug;
            var counter = 1;
            while (!await IsSlugAvailableAsync(organization.Slug))
                organization.Slug = $"{baseSlug}-{counter++}";
        }
        organization.CreatedBy = userId;
        organization.CreatedDate = DateTime.UtcNow;
        organization.Status = "Active";

        var entity = new Organization();
        ApplyAll(organization, entity);
        db.Organizations.Add(entity);
        if (!await db.Users.AnyAsync(u => u.UserId == userId)) db.Users.Add(new User { UserId = userId });
        await db.SaveChangesAsync();

        db.OrganizationAdmins.Add(new OrganizationAdmin { OrganizationId = entity.Id, UserId = userId, Since = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        organization.Id = entity.Id;
        return organization;
    }

    public async Task<OrganizationModel?> GetOrganizationBySlugAsync(string slug, string? userId = null)
    {
        var o = await db.Organizations.FirstOrDefaultAsync(x => x.Slug == slug);
        if (o is null) return null;

        var model = MapFull(o);
        var memberIds = db.OrganizationMembers.Where(m => m.OrganizationId == o.Id).Select(m => m.UserId);
        var adminIds = db.OrganizationAdmins.Where(a => a.OrganizationId == o.Id).Select(a => a.UserId);
        model.MemberCount = await memberIds.Union(adminIds).CountAsync();
        model.EventCount = await db.GoodWorks.CountAsync(g => g.OrganizationId == o.Id);
        model.VolunteerCount = await db.Signups.Where(s => s.GoodWork.OrganizationId == o.Id).Select(s => s.UserId).Distinct().CountAsync();

        if (!string.IsNullOrEmpty(userId))
        {
            model.IsUserAdmin = await db.OrganizationAdmins.AnyAsync(a => a.OrganizationId == o.Id && a.UserId == userId);
            model.IsUserMember = model.IsUserAdmin || await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == o.Id && m.UserId == userId);
        }
        return model;
    }

    public async Task<List<OrganizationModel>> GetOrganizationsAsync(int skip = 0, int limit = 20, string? searchTerm = null)
    {
        var term = searchTerm?.ToLower();
        var orgs = await db.Organizations
            .Where(o => o.Status == "Active" && (term == null
                || o.Name.ToLower().Contains(term) || o.Description.ToLower().Contains(term)
                || (o.City != null && o.City.ToLower().Contains(term))))
            .OrderByDescending(o => o.CreatedDate).Skip(skip).Take(limit).ToListAsync();

        return await WithCounts(orgs);
    }

    public async Task<int> CountOrganizationsAsync(string? searchTerm = null)
    {
        var term = searchTerm?.ToLower();
        return await db.Organizations.CountAsync(o => o.Status == "Active" && (term == null
            || o.Name.ToLower().Contains(term) || o.Description.ToLower().Contains(term)
            || (o.City != null && o.City.ToLower().Contains(term))));
    }

    public async Task<List<OrganizationModel>> GetUserOrganizationsAsync(string userId)
    {
        var adminOrgIds = await db.OrganizationAdmins.Where(a => a.UserId == userId).Select(a => a.OrganizationId).ToListAsync();
        var memberOrgIds = await db.OrganizationMembers.Where(m => m.UserId == userId).Select(m => m.OrganizationId).ToListAsync();
        var ids = adminOrgIds.Concat(memberOrgIds).Distinct().ToList();

        var orgs = await db.Organizations.Where(o => ids.Contains(o.Id) && o.Status == "Active")
            .OrderBy(o => o.Name).ToListAsync();
        var models = await WithCounts(orgs);
        foreach (var m in models)
        {
            m.IsUserAdmin = adminOrgIds.Contains(m.Id!.Value);
            m.IsUserMember = true;
        }
        return models;
    }

    public async Task<bool> AddMemberAsync(long organizationId, string userId, string role = "Member")
    {
        if (!await db.Users.AnyAsync(u => u.UserId == userId)) db.Users.Add(new User { UserId = userId });
        var row = await db.OrganizationMembers.FindAsync(userId, organizationId);
        if (row is null)
            db.OrganizationMembers.Add(new OrganizationMember { OrganizationId = organizationId, UserId = userId, Role = role, JoinedDate = DateTimeOffset.UtcNow });
        else row.Role = role;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberAsync(long organizationId, string userId) =>
        await db.OrganizationMembers.Where(m => m.OrganizationId == organizationId && m.UserId == userId).ExecuteDeleteAsync() > 0;

    public async Task<List<OrganizationMemberModel>> GetOrganizationMembersAsync(long organizationId)
    {
        var admins = await db.OrganizationAdmins.Where(a => a.OrganizationId == organizationId)
            .Select(a => new OrganizationMemberModel
            {
                UserId = a.UserId, Name = a.User.Name ?? "Unknown", Email = a.User.Email ?? "",
                Phone = a.User.Phone, Role = "Admin", JoinedDate = a.Since.UtcDateTime,
            }).ToListAsync();
        var members = await db.OrganizationMembers.Where(m => m.OrganizationId == organizationId)
            .Select(m => new OrganizationMemberModel
            {
                UserId = m.UserId, Name = m.User.Name ?? "Unknown", Email = m.User.Email ?? "",
                Phone = m.User.Phone, Role = m.Role, JoinedDate = m.JoinedDate.UtcDateTime,
            }).ToListAsync();

        // ContributedEvents came from a (u)-[:CREATED]->(gw)-[:POSTED_BY]->(o) path; the CREATED edge
        // was never written, so this was always 0. Preserved. (One line to enable via GoodWork.CreatedBy.)
        var all = admins.Concat(members.Where(m => admins.All(a => a.UserId != m.UserId)));
        return all.OrderByDescending(x => x.Role == "Admin").ThenBy(x => x.JoinedDate).ToList();
    }

    public async Task<bool> UpdateOrganizationAsync(OrganizationModel organization)
    {
        var o = await db.Organizations.FindAsync(organization.Id);
        if (o is null) return false;
        organization.LastModifiedDate = DateTime.UtcNow;
        // NOTE: taxId, foundedDate, isVerified are intentionally NOT updated (matches old Cypher).
        o.Name = organization.Name;
        o.Description = organization.Description;
        o.Mission = organization.Mission;
        o.Vision = organization.Vision;
        o.ContactEmail = organization.ContactEmail;
        o.ContactPhone = organization.ContactPhone;
        o.Website = organization.Website;
        o.Address = organization.Address;
        o.City = organization.City;
        o.State = organization.State;
        o.Country = organization.Country;
        o.ZipCode = organization.ZipCode;
        o.Latitude = organization.Latitude;
        o.Longitude = organization.Longitude;
        o.OrganizationType = organization.OrganizationType;
        o.LogoUrl = organization.LogoUrl;
        o.CoverImageUrl = organization.CoverImageUrl;
        o.FacebookUrl = organization.FacebookUrl;
        o.TwitterUrl = organization.TwitterUrl;
        o.InstagramUrl = organization.InstagramUrl;
        o.LinkedInUrl = organization.LinkedInUrl;
        o.FocusAreas = organization.FocusAreas ?? new();
        o.Tags = organization.Tags ?? new();
        o.LastModifiedDate = ToDto(organization.LastModifiedDate);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOrganizationAsync(long organizationId)
    {
        var o = await db.Organizations.FindAsync(organizationId);
        if (o is null) return false;
        o.Status = "Inactive";
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsUserAdminAsync(long organizationId, string userId) =>
        await db.OrganizationAdmins.AnyAsync(a => a.OrganizationId == organizationId && a.UserId == userId);

    // Test-data helpers (were inline Cypher in TestDataService). Org tags are a text[] column.
    public async Task<int> DeleteByTagAsync(string marker)
    {
        var ids = await db.Organizations.Where(o => o.Tags.Contains(marker)).Select(o => o.Id).ToListAsync();
        await db.Organizations.Where(o => ids.Contains(o.Id)).ExecuteDeleteAsync();
        return ids.Count;
    }

    public Task<int> CountByTagAsync(string marker) =>
        db.Organizations.CountAsync(o => o.Tags.Contains(marker));

    public async Task<bool> PromoteToAdminAsync(long organizationId, string userId)
    {
        await db.OrganizationMembers.Where(m => m.OrganizationId == organizationId && m.UserId == userId).ExecuteDeleteAsync();
        if (!await db.OrganizationAdmins.AnyAsync(a => a.OrganizationId == organizationId && a.UserId == userId))
        {
            if (!await db.Users.AnyAsync(u => u.UserId == userId)) db.Users.Add(new User { UserId = userId });
            db.OrganizationAdmins.Add(new OrganizationAdmin { OrganizationId = organizationId, UserId = userId, Since = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        return true;
    }

    // ---- mapping helpers ----
    private async Task<List<OrganizationModel>> WithCounts(List<Organization> orgs)
    {
        var result = new List<OrganizationModel>();
        foreach (var o in orgs)
        {
            var m = MapFull(o);
            m.MemberCount = await db.OrganizationMembers.CountAsync(x => x.OrganizationId == o.Id)
                            + await db.OrganizationAdmins.CountAsync(x => x.OrganizationId == o.Id);
            m.EventCount = await db.GoodWorks.CountAsync(g => g.OrganizationId == o.Id);
            result.Add(m);
        }
        return result;
    }

    private static OrganizationModel MapFull(Organization o) => new()
    {
        Id = o.Id, Name = o.Name, Slug = o.Slug, Description = o.Description, Mission = o.Mission, Vision = o.Vision,
        ContactEmail = o.ContactEmail, ContactPhone = o.ContactPhone, Website = o.Website, Address = o.Address,
        City = o.City, State = o.State, Country = o.Country, ZipCode = o.ZipCode, Latitude = o.Latitude, Longitude = o.Longitude,
        OrganizationType = o.OrganizationType, TaxId = o.TaxId, FoundedDate = FromDto(o.FoundedDate),
        LogoUrl = o.LogoUrl, CoverImageUrl = o.CoverImageUrl, FacebookUrl = o.FacebookUrl, TwitterUrl = o.TwitterUrl,
        InstagramUrl = o.InstagramUrl, LinkedInUrl = o.LinkedInUrl,
        FocusAreas = o.FocusAreas.Count == 0 ? null : o.FocusAreas, Tags = o.Tags.Count == 0 ? null : o.Tags,
        CreatedBy = o.CreatedBy ?? "", CreatedDate = o.CreatedDate.UtcDateTime, LastModifiedDate = FromDto(o.LastModifiedDate),
        Status = o.Status, IsVerified = o.IsVerified,
    };

    private static void ApplyAll(OrganizationModel m, Organization o)
    {
        o.Name = m.Name; o.Slug = m.Slug; o.Description = m.Description; o.Mission = m.Mission; o.Vision = m.Vision;
        o.ContactEmail = m.ContactEmail; o.ContactPhone = m.ContactPhone; o.Website = m.Website; o.Address = m.Address;
        o.City = m.City; o.State = m.State; o.Country = m.Country; o.ZipCode = m.ZipCode; o.Latitude = m.Latitude; o.Longitude = m.Longitude;
        o.OrganizationType = m.OrganizationType; o.TaxId = m.TaxId; o.FoundedDate = ToDto(m.FoundedDate);
        o.LogoUrl = m.LogoUrl; o.CoverImageUrl = m.CoverImageUrl; o.FacebookUrl = m.FacebookUrl; o.TwitterUrl = m.TwitterUrl;
        o.InstagramUrl = m.InstagramUrl; o.LinkedInUrl = m.LinkedInUrl;
        o.FocusAreas = m.FocusAreas ?? new(); o.Tags = m.Tags ?? new();
        o.CreatedBy = m.CreatedBy; o.CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(m.CreatedDate, DateTimeKind.Utc));
        o.LastModifiedDate = ToDto(m.LastModifiedDate); o.Status = m.Status; o.IsVerified = m.IsVerified;
    }
}
