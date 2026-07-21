using CharityRabbit.Models;

namespace CharityRabbit.Data;

/// <summary>
/// Good-works data access shared by the server (EF Core implementation) and the
/// mobile app (HTTP implementation calling /api/v1).
/// <para>
/// <c>userId</c>/<c>userName</c>/<c>userEmail</c> parameters exist for the server
/// implementation, which receives the authenticated subject from the caller.
/// HTTP implementations ignore these arguments — the API derives identity from the
/// bearer token and never trusts client-sent identity.
/// </para>
/// </summary>
public interface IGoodWorksService
{
    Task CreateGoodWorkAsync(GoodWorksModel goodWork, string? userId = null);
    Task<GoodWorksModel?> GetGoodWorkByIdAsync(long id, string? userId = null);
    Task<List<GoodWorksModel>> SearchGoodWorksAsync(GoodWorksSearchCriteria criteria, string? userId = null);
    Task<int> CountSearchResultsAsync(GoodWorksSearchCriteria criteria);
    Task<List<GoodWorksModel>> GetSimilarGoodWorksAsync(long goodWorkId, string? userId = null, int limit = 10);
    Task MarkUserInterestedAsync(string userId, long goodWorkId, bool interested, string? userName = null, string? userEmail = null);
    Task SignUpUserAsync(string userId, long goodWorkId, bool signUp, string? userName = null, string? userEmail = null);
    Task<List<GoodWorksModel>> GetUserInterestedGoodWorksAsync(string userId);
    Task<List<GoodWorksModel>> GetUserSignedUpGoodWorksAsync(string userId);
    Task<List<GoodWorksModel>> GetUserCreatedGoodWorksAsync(string userId);
    Task<List<GoodWorksModel>> GetUserCreatedGoodWorksPagedAsync(string userId, int page = 1, int pageSize = 10, string? searchText = null);
    Task<int> CountUserCreatedGoodWorksAsync(string userId, string? searchText = null);
    Task UpdateGoodWorkAsync(long id, GoodWorksModel goodWork, string userId);
    Task DeleteGoodWorkAsync(long id, string userId);
    Task<List<GoodWorksModel>> GetOrganizationEventsAsync(long organizationId);
    Task SetGoodWorkOrganizationAsync(long goodWorkId, long organizationId);
    Task<List<GoodWorksModel>> GetGoodWorksInBoundsAsync(double minLat, double maxLat, double minLng, double maxLng);
    Task<List<GoodWorksModel>> GetUpcomingUserEventsAsync(string userId);
    Task<List<GoodWorksModel>> GetRecommendedGoodWorksAsync(string userId, int limit = 10);
    Task<List<GoodWorksModel>> GetNewOpportunitiesAsync(int daysBack = 30, int limit = 12);
    Task<List<DoGooderModel>> GetActiveDoGoodersAsync(int limit = 10);
    Task<List<ParticipantModel>> GetGoodWorkParticipantsAsync(long goodWorkId);
}
