using CharityRabbit.Data;
using CharityRabbit.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace CharityRabbit.Maui.Services;

/// <summary>Client-side mirror of the API's paged envelope.</summary>
internal sealed record PagedResultDto<T>(List<T> Items, int TotalCount, int Page, int PageSize);

internal sealed record AvailabilityDto(bool Available);
internal sealed record EngagementRequestDto(bool Value);
internal sealed record SetOrganizationRequestDto(long OrganizationId);
internal sealed record AddMemberRequestDto(string UserId);

/// <summary>
/// HTTP implementations of the shared data interfaces, calling /api/v1. Identity comes
/// exclusively from the bearer token attached by AccessTokenHandler — the userId parameters
/// on the interfaces are ignored here by design (see interface docs), EXCEPT where a
/// parameter designates a target member (org membership management).
/// </summary>
public class GoodWorksApiClient(HttpClient http) : IGoodWorksService
{
    public async Task CreateGoodWorkAsync(GoodWorksModel goodWork, string? userId = null)
    {
        var response = await http.PostAsJsonAsync("/api/v1/goodworks", goodWork);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<GoodWorksModel>();
        goodWork.Id = created?.Id; // callers read the id back off the model, matching server behavior
    }

    public async Task<GoodWorksModel?> GetGoodWorkByIdAsync(long id, string? userId = null)
    {
        var response = await http.GetAsync($"/api/v1/goodworks/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GoodWorksModel>();
    }

    public async Task<List<GoodWorksModel>> SearchGoodWorksAsync(GoodWorksSearchCriteria criteria, string? userId = null)
    {
        var response = await http.PostAsJsonAsync("/api/v1/goodworks/search", criteria);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<GoodWorksModel>>();
        return page?.Items ?? [];
    }

    public async Task<int> CountSearchResultsAsync(GoodWorksSearchCriteria criteria)
    {
        // Same endpoint, minimal page — the envelope carries the total.
        var probe = new GoodWorksSearchCriteria
        {
            Category = criteria.Category,
            SubCategory = criteria.SubCategory,
            Tags = criteria.Tags,
            CenterLatitude = criteria.CenterLatitude,
            CenterLongitude = criteria.CenterLongitude,
            RadiusMiles = criteria.RadiusMiles,
            StartDateFrom = criteria.StartDateFrom,
            StartDateTo = criteria.StartDateTo,
            EffortLevel = criteria.EffortLevel,
            IsVirtual = criteria.IsVirtual,
            IsAccessible = criteria.IsAccessible,
            FamilyFriendly = criteria.FamilyFriendly,
            RequiredSkills = criteria.RequiredSkills,
            HasAvailableSpots = criteria.HasAvailableSpots,
            SearchText = criteria.SearchText,
            Page = 1,
            PageSize = 1,
        };
        var response = await http.PostAsJsonAsync("/api/v1/goodworks/search", probe);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<GoodWorksModel>>();
        return page?.TotalCount ?? 0;
    }

    public async Task<List<GoodWorksModel>> GetSimilarGoodWorksAsync(long goodWorkId, string? userId = null, int limit = 10) =>
        await http.GetFromJsonAsync<List<GoodWorksModel>>($"/api/v1/goodworks/{goodWorkId}/similar?limit={limit}") ?? [];

    public async Task MarkUserInterestedAsync(string userId, long goodWorkId, bool interested, string? userName = null, string? userEmail = null) =>
        (await http.PutAsJsonAsync($"/api/v1/goodworks/{goodWorkId}/interest", new EngagementRequestDto(interested))).EnsureSuccessStatusCode();

    public async Task SignUpUserAsync(string userId, long goodWorkId, bool signUp, string? userName = null, string? userEmail = null) =>
        (await http.PutAsJsonAsync($"/api/v1/goodworks/{goodWorkId}/signup", new EngagementRequestDto(signUp))).EnsureSuccessStatusCode();

    public async Task<List<GoodWorksModel>> GetUserInterestedGoodWorksAsync(string userId) =>
        await http.GetFromJsonAsync<List<GoodWorksModel>>("/api/v1/me/goodworks/interested") ?? [];

    public async Task<List<GoodWorksModel>> GetUserSignedUpGoodWorksAsync(string userId) =>
        await http.GetFromJsonAsync<List<GoodWorksModel>>("/api/v1/me/goodworks/signedup") ?? [];

    public async Task<List<GoodWorksModel>> GetUserCreatedGoodWorksAsync(string userId) =>
        await GetUserCreatedGoodWorksPagedAsync(userId, 1, 100);

    public async Task<List<GoodWorksModel>> GetUserCreatedGoodWorksPagedAsync(string userId, int page = 1, int pageSize = 10, string? searchText = null)
    {
        var url = $"/api/v1/me/goodworks/created?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(searchText)) url += $"&search={Uri.EscapeDataString(searchText)}";
        var result = await http.GetFromJsonAsync<PagedResultDto<GoodWorksModel>>(url);
        return result?.Items ?? [];
    }

    public async Task<int> CountUserCreatedGoodWorksAsync(string userId, string? searchText = null)
    {
        var url = "/api/v1/me/goodworks/created?page=1&pageSize=1";
        if (!string.IsNullOrEmpty(searchText)) url += $"&search={Uri.EscapeDataString(searchText)}";
        var result = await http.GetFromJsonAsync<PagedResultDto<GoodWorksModel>>(url);
        return result?.TotalCount ?? 0;
    }

    public async Task UpdateGoodWorkAsync(long id, GoodWorksModel goodWork, string userId) =>
        (await http.PutAsJsonAsync($"/api/v1/goodworks/{id}", goodWork)).EnsureSuccessStatusCode();

    public async Task DeleteGoodWorkAsync(long id, string userId) =>
        (await http.DeleteAsync($"/api/v1/goodworks/{id}")).EnsureSuccessStatusCode();

    public async Task<List<GoodWorksModel>> GetOrganizationEventsAsync(long organizationId) =>
        await http.GetFromJsonAsync<List<GoodWorksModel>>($"/api/v1/organizations/{organizationId}/events") ?? [];

    public async Task SetGoodWorkOrganizationAsync(long goodWorkId, long organizationId) =>
        (await http.PutAsJsonAsync($"/api/v1/goodworks/{goodWorkId}/organization", new SetOrganizationRequestDto(organizationId))).EnsureSuccessStatusCode();

    public async Task<List<GoodWorksModel>> GetGoodWorksInBoundsAsync(double minLat, double maxLat, double minLng, double maxLng) =>
        await http.GetFromJsonAsync<List<GoodWorksModel>>(FormattableString.Invariant(
            $"/api/v1/goodworks/in-bounds?minLat={minLat}&maxLat={maxLat}&minLng={minLng}&maxLng={maxLng}")) ?? [];

    public async Task<List<GoodWorksModel>> GetUpcomingUserEventsAsync(string userId) =>
        await http.GetFromJsonAsync<List<GoodWorksModel>>("/api/v1/me/goodworks/upcoming") ?? [];

    public async Task<List<GoodWorksModel>> GetRecommendedGoodWorksAsync(string userId, int limit = 10) =>
        await http.GetFromJsonAsync<List<GoodWorksModel>>($"/api/v1/me/recommendations?limit={limit}") ?? [];

    public async Task<List<GoodWorksModel>> GetNewOpportunitiesAsync(int daysBack = 30, int limit = 12) =>
        await http.GetFromJsonAsync<List<GoodWorksModel>>($"/api/v1/goodworks/new?daysBack={daysBack}&limit={limit}") ?? [];

    public async Task<List<DoGooderModel>> GetActiveDoGoodersAsync(int limit = 10) =>
        await http.GetFromJsonAsync<List<DoGooderModel>>($"/api/v1/dogooders?limit={limit}") ?? [];

    public async Task<List<ParticipantModel>> GetGoodWorkParticipantsAsync(long goodWorkId) =>
        await http.GetFromJsonAsync<List<ParticipantModel>>($"/api/v1/goodworks/{goodWorkId}/participants") ?? [];
}

public class OrganizationsApiClient(HttpClient http, AuthenticationStateProvider authState) : IOrganizationService
{
    private async Task<string?> CurrentUserIdAsync()
    {
        var state = await authState.GetAuthenticationStateAsync();
        return state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? state.User.FindFirst("sub")?.Value;
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, long? excludeOrgId = null)
    {
        var url = $"/api/v1/organizations/slug-available?slug={Uri.EscapeDataString(slug)}";
        if (excludeOrgId is not null) url += $"&excludeId={excludeOrgId}";
        var result = await http.GetFromJsonAsync<AvailabilityDto>(url);
        return result?.Available ?? false;
    }

    public async Task<OrganizationModel> CreateOrganizationAsync(OrganizationModel organization, string userId)
    {
        var response = await http.PostAsJsonAsync("/api/v1/organizations", organization);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrganizationModel>() ?? organization;
    }

    public async Task<OrganizationModel?> GetOrganizationBySlugAsync(string slug, string? userId = null)
    {
        var response = await http.GetAsync($"/api/v1/organizations/{Uri.EscapeDataString(slug)}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrganizationModel>();
    }

    public async Task<List<OrganizationModel>> GetOrganizationsAsync(int skip = 0, int limit = 20, string? searchTerm = null)
    {
        var page = limit > 0 ? skip / limit + 1 : 1;
        var url = $"/api/v1/organizations?page={page}&pageSize={limit}";
        if (!string.IsNullOrEmpty(searchTerm)) url += $"&search={Uri.EscapeDataString(searchTerm)}";
        var result = await http.GetFromJsonAsync<PagedResultDto<OrganizationModel>>(url);
        return result?.Items ?? [];
    }

    public async Task<int> CountOrganizationsAsync(string? searchTerm = null)
    {
        var url = "/api/v1/organizations?page=1&pageSize=1";
        if (!string.IsNullOrEmpty(searchTerm)) url += $"&search={Uri.EscapeDataString(searchTerm)}";
        var result = await http.GetFromJsonAsync<PagedResultDto<OrganizationModel>>(url);
        return result?.TotalCount ?? 0;
    }

    public async Task<List<OrganizationModel>> GetUserOrganizationsAsync(string userId) =>
        await http.GetFromJsonAsync<List<OrganizationModel>>("/api/v1/me/organizations") ?? [];

    public async Task<bool> AddMemberAsync(long organizationId, string userId, string role = "Member")
    {
        // Self-join and admin-add are different endpoints with different authorization.
        var isSelf = userId == await CurrentUserIdAsync();
        var response = isSelf
            ? await http.PostAsync($"/api/v1/organizations/{organizationId}/join", null)
            : await http.PostAsJsonAsync($"/api/v1/organizations/{organizationId}/members", new AddMemberRequestDto(userId));
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveMemberAsync(long organizationId, string userId)
    {
        var isSelf = userId == await CurrentUserIdAsync();
        var response = isSelf
            ? await http.DeleteAsync($"/api/v1/organizations/{organizationId}/membership")
            : await http.DeleteAsync($"/api/v1/organizations/{organizationId}/members/{Uri.EscapeDataString(userId)}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<OrganizationMemberModel>> GetOrganizationMembersAsync(long organizationId) =>
        await http.GetFromJsonAsync<List<OrganizationMemberModel>>($"/api/v1/organizations/{organizationId}/members") ?? [];

    public async Task<bool> UpdateOrganizationAsync(OrganizationModel organization)
    {
        if (organization.Id is null) return false;
        var response = await http.PutAsJsonAsync($"/api/v1/organizations/{organization.Id}", organization);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteOrganizationAsync(long organizationId) =>
        (await http.DeleteAsync($"/api/v1/organizations/{organizationId}")).IsSuccessStatusCode;

    public async Task<bool> IsUserAdminAsync(long organizationId, string userId)
    {
        // No dedicated endpoint; /me/organizations carries the caller's admin flags.
        var mine = await GetUserOrganizationsAsync(userId);
        return mine.Any(o => o.Id == organizationId && o.IsUserAdmin);
    }

    public async Task<bool> PromoteToAdminAsync(long organizationId, string userId) =>
        (await http.PostAsync($"/api/v1/organizations/{organizationId}/members/{Uri.EscapeDataString(userId)}/promote", null)).IsSuccessStatusCode;
}

public class SkillsApiClient(HttpClient http) : ISkillService
{
    public async Task<List<SkillModel>> GetAllSkillsAsync() =>
        await http.GetFromJsonAsync<List<SkillModel>>("/api/v1/skills") ?? [];

    public async Task<List<SkillCategoryModel>> GetSkillsByCategoryAsync() =>
        await http.GetFromJsonAsync<List<SkillCategoryModel>>("/api/v1/skills/categories") ?? [];

    public async Task<List<SkillModel>> SearchSkillsAsync(string searchTerm) =>
        await http.GetFromJsonAsync<List<SkillModel>>($"/api/v1/skills/search?q={Uri.EscapeDataString(searchTerm)}") ?? [];

    public async Task<bool> AddUserSkillAsync(string userId, string skillName) =>
        (await http.PutAsync($"/api/v1/me/skills/{Uri.EscapeDataString(skillName)}", null)).IsSuccessStatusCode;

    public async Task<bool> RemoveUserSkillAsync(string userId, string skillName) =>
        (await http.DeleteAsync($"/api/v1/me/skills/{Uri.EscapeDataString(skillName)}")).IsSuccessStatusCode;

    public async Task<List<string>> GetUserSkillsAsync(string userId) =>
        await http.GetFromJsonAsync<List<string>>("/api/v1/me/skills") ?? [];

    public Dictionary<string, List<string>> GetPredefinedSkills() => PredefinedSkills.All;
}

public class GeoApiClient(HttpClient http) : IGeocodingService, IPlacesService
{
    public async Task<GeoPoint> GetCoordinatesAsync(string address) =>
        await http.GetFromJsonAsync<GeoPoint>($"/api/v1/geo/geocode?address={Uri.EscapeDataString(address)}")
        ?? throw new Exception("Unable to geocode the address.");

    public async Task<LocationDetails> GetLocationDetailsAsync(double lat, double lng) =>
        await http.GetFromJsonAsync<LocationDetails>(FormattableString.Invariant($"/api/v1/geo/reverse?lat={lat}&lng={lng}"))
        ?? throw new Exception("Unable to reverse-geocode the location.");

    public async Task<List<string>> GetAddressSuggestionsAsync(string input) =>
        string.IsNullOrEmpty(input) || input.Length < 3
            ? []
            : await http.GetFromJsonAsync<List<string>>($"/api/v1/geo/places?input={Uri.EscapeDataString(input)}") ?? [];
}
