using CharityRabbit.Auth;
using CharityRabbit.Data;
using CharityRabbit.Models;
using System.Security.Claims;

namespace CharityRabbit.Api;

public static class GoodWorksEndpoints
{
    public static void MapGoodWorksApi(this IEndpointRouteBuilder api)
    {
        // ---- Public reads (personalized when a bearer token is presented) ----
        var pub = api.MapGroup("/goodworks")
            .WithTags("GoodWorks")
            .AllowAnonymous()
            .AddEndpointFilter<OptionalBearerAuthFilter>();

        // Criteria as a POST body: 16 fields including two lists is unworkable as query params.
        pub.MapPost("/search", async (GoodWorksSearchCriteria criteria, ClaimsPrincipal user, IGoodWorksService goodWorks) =>
        {
            criteria.Page = Math.Max(1, criteria.Page);
            criteria.PageSize = Math.Clamp(criteria.PageSize, 1, 100);
            var items = await goodWorks.SearchGoodWorksAsync(criteria, user.GetUserIdOrNull());
            // An under-full first page already tells us the total — skip re-running the
            // (expensive) search predicate as a COUNT.
            var total = criteria.Page == 1 && items.Count < criteria.PageSize
                ? items.Count
                : await goodWorks.CountSearchResultsAsync(criteria);
            return new PagedResult<GoodWorksModel>(items, total, criteria.Page, criteria.PageSize);
        }).WithName("SearchGoodWorks");

        pub.MapGet("/{id:long}", async (long id, ClaimsPrincipal user, IGoodWorksService goodWorks) =>
            await goodWorks.GetGoodWorkByIdAsync(id, user.GetUserIdOrNull()) is { } gw
                ? Results.Ok(gw)
                : Results.NotFound())
            .WithName("GetGoodWork");

        pub.MapGet("/{id:long}/similar", async (long id, int? limit, ClaimsPrincipal user, IGoodWorksService goodWorks) =>
            await goodWorks.GetSimilarGoodWorksAsync(id, user.GetUserIdOrNull(), Math.Clamp(limit ?? 10, 1, 25)))
            .WithName("GetSimilarGoodWorks");

        pub.MapGet("/new", async (int? daysBack, int? limit, IGoodWorksService goodWorks) =>
            await goodWorks.GetNewOpportunitiesAsync(Math.Clamp(daysBack ?? 30, 1, 365), Math.Clamp(limit ?? 12, 1, 50)))
            .WithName("GetNewOpportunities");

        pub.MapGet("/in-bounds", async (double minLat, double maxLat, double minLng, double maxLng, IGoodWorksService goodWorks) =>
            await goodWorks.GetGoodWorksInBoundsAsync(minLat, maxLat, minLng, maxLng))
            .WithName("GetGoodWorksInBounds");

        api.MapGet("/dogooders", async (int? limit, IGoodWorksService goodWorks) =>
            await goodWorks.GetActiveDoGoodersAsync(Math.Clamp(limit ?? 10, 1, 50)))
            .WithTags("GoodWorks").AllowAnonymous().WithName("GetActiveDoGooders");

        // ---- Writes (bearer required; identity comes from the token, never the body) ----
        var auth = api.MapGroup("/goodworks")
            .WithTags("GoodWorks")
            .RequireAuthorization("Api");

        auth.MapPost("/", async (GoodWorksModel model, ClaimsPrincipal user, IGoodWorksService goodWorks) =>
        {
            var sub = user.GetRequiredUserId();
            model.Id = null;
            model.CreatedBy = sub;
            model.CreatedDate = DateTime.UtcNow;
            model.CurrentParticipants = 0; // server-tracked; a body value would fake demand or block signups
            model.Status = "Active";
            await goodWorks.CreateGoodWorkAsync(model, sub);
            return Results.Created($"/api/v1/goodworks/{model.Id}", model);
        }).WithName("CreateGoodWork");

        // Ownership checks use GetOwnershipAsync on the concrete (server-only) service — a
        // single-row projection instead of the full aggregate load. The pre-check exists
        // because Update/Delete silently no-op on CreatedBy mismatch; it turns that into
        // honest 404/403 responses.
        auth.MapPut("/{id:long}", async (long id, GoodWorksModel model, ClaimsPrincipal user, GoodWorksService goodWorks) =>
        {
            var sub = user.GetRequiredUserId();
            if (await RequireOwnerAsync(goodWorks, id, sub) is { } error) return error;
            await goodWorks.UpdateGoodWorkAsync(id, model, sub);
            return Results.NoContent();
        }).WithName("UpdateGoodWork");

        auth.MapDelete("/{id:long}", async (long id, ClaimsPrincipal user, GoodWorksService goodWorks) =>
        {
            var sub = user.GetRequiredUserId();
            if (await RequireOwnerAsync(goodWorks, id, sub) is { } error) return error;
            await goodWorks.DeleteGoodWorkAsync(id, sub);
            return Results.NoContent();
        }).WithName("DeleteGoodWork");

        auth.MapPut("/{id:long}/interest", async (long id, EngagementRequest request, ClaimsPrincipal user, IGoodWorksService goodWorks) =>
        {
            // Name/email come from token claims — client-sent values could poison the shared users row.
            await goodWorks.MarkUserInterestedAsync(user.GetRequiredUserId(), id, request.Value, user.GetDisplayName(), user.GetEmail());
            return Results.NoContent();
        }).WithName("SetInterest");

        auth.MapPut("/{id:long}/signup", async (long id, EngagementRequest request, ClaimsPrincipal user, IGoodWorksService goodWorks) =>
        {
            await goodWorks.SignUpUserAsync(user.GetRequiredUserId(), id, request.Value, user.GetDisplayName(), user.GetEmail());
            return Results.NoContent();
        }).WithName("SetSignup");

        auth.MapPut("/{id:long}/organization", async (long id, SetOrganizationRequest request, ClaimsPrincipal user,
            GoodWorksService goodWorks, IOrganizationService organizations) =>
        {
            var sub = user.GetRequiredUserId();
            if (await RequireOwnerAsync(goodWorks, id, sub) is { } error) return error;
            if (!await organizations.IsUserAdminAsync(request.OrganizationId, sub)) return ApiResults.Forbidden();
            await goodWorks.SetGoodWorkOrganizationAsync(id, request.OrganizationId);
            return Results.NoContent();
        }).WithName("SetGoodWorkOrganization");

        // Participants carry emails/phones (PII): event creator or admin of its organization only.
        auth.MapGet("/{id:long}/participants", async (long id, ClaimsPrincipal user,
            GoodWorksService goodWorks, IOrganizationService organizations) =>
        {
            var sub = user.GetRequiredUserId();
            var ownership = await goodWorks.GetOwnershipAsync(id);
            if (ownership is null) return Results.NotFound();
            var allowed = ownership.CreatedBy == sub
                || (ownership.OrganizationId is long orgId && await organizations.IsUserAdminAsync(orgId, sub));
            if (!allowed) return ApiResults.Forbidden();
            return Results.Ok(await goodWorks.GetGoodWorkParticipantsAsync(id));
        }).WithName("GetGoodWorkParticipants");
    }

    /// <summary>404 if the good work doesn't exist, 403 if the caller didn't create it,
    /// null when the caller may proceed.</summary>
    private static async Task<IResult?> RequireOwnerAsync(GoodWorksService goodWorks, long id, string sub)
    {
        var ownership = await goodWorks.GetOwnershipAsync(id);
        if (ownership is null) return Results.NotFound();
        if (ownership.CreatedBy != sub) return ApiResults.Forbidden();
        return null;
    }
}
