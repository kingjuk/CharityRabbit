using CharityRabbit.Auth;
using CharityRabbit.Data;
using CharityRabbit.Models;
using System.Security.Claims;

namespace CharityRabbit.Api;

/// <summary>"My data" endpoints — the subject always comes from the bearer token.</summary>
public static class MeEndpoints
{
    public static void MapMeApi(this IEndpointRouteBuilder api)
    {
        var me = api.MapGroup("/me").WithTags("Me").RequireAuthorization("Api");

        me.MapGet("/", (ClaimsPrincipal user) =>
            new MeResponse(user.GetRequiredUserId(), user.GetDisplayName(), user.GetEmail()))
            .WithName("GetMe");

        me.MapGet("/goodworks/created", async (int? page, int? pageSize, string? search, ClaimsPrincipal user, IGoodWorksService goodWorks) =>
        {
            var sub = user.GetRequiredUserId();
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 10, 1, 100);
            var items = await goodWorks.GetUserCreatedGoodWorksPagedAsync(sub, p, ps, search);
            var total = p == 1 && items.Count < ps ? items.Count : await goodWorks.CountUserCreatedGoodWorksAsync(sub, search);
            return new PagedResult<GoodWorksModel>(items, total, p, ps);
        }).WithName("GetMyCreatedGoodWorks");

        me.MapGet("/goodworks/interested", async (ClaimsPrincipal user, IGoodWorksService goodWorks) =>
            await goodWorks.GetUserInterestedGoodWorksAsync(user.GetRequiredUserId()))
            .WithName("GetMyInterestedGoodWorks");

        me.MapGet("/goodworks/signedup", async (ClaimsPrincipal user, IGoodWorksService goodWorks) =>
            await goodWorks.GetUserSignedUpGoodWorksAsync(user.GetRequiredUserId()))
            .WithName("GetMySignedUpGoodWorks");

        me.MapGet("/goodworks/upcoming", async (ClaimsPrincipal user, IGoodWorksService goodWorks) =>
            await goodWorks.GetUpcomingUserEventsAsync(user.GetRequiredUserId()))
            .WithName("GetMyUpcomingEvents");

        me.MapGet("/recommendations", async (int? limit, ClaimsPrincipal user, IGoodWorksService goodWorks) =>
            await goodWorks.GetRecommendedGoodWorksAsync(user.GetRequiredUserId(), Math.Clamp(limit ?? 10, 1, 50)))
            .WithName("GetMyRecommendations");

        me.MapGet("/organizations", async (ClaimsPrincipal user, IOrganizationService organizations) =>
            await organizations.GetUserOrganizationsAsync(user.GetRequiredUserId()))
            .WithName("GetMyOrganizations");

        me.MapGet("/skills", async (ClaimsPrincipal user, ISkillService skillService) =>
            await skillService.GetUserSkillsAsync(user.GetRequiredUserId()))
            .WithName("GetMySkills");

        me.MapPut("/skills/{name}", async (string name, ClaimsPrincipal user, ISkillService skillService) =>
            await skillService.AddUserSkillAsync(user.GetRequiredUserId(), name)
                ? Results.NoContent()
                : Results.BadRequest())
            .WithName("AddMySkill");

        me.MapDelete("/skills/{name}", async (string name, ClaimsPrincipal user, ISkillService skillService) =>
            await skillService.RemoveUserSkillAsync(user.GetRequiredUserId(), name)
                ? Results.NoContent()
                : Results.NotFound())
            .WithName("RemoveMySkill");
    }
}
