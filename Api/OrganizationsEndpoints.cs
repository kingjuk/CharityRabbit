using CharityRabbit.Auth;
using CharityRabbit.Data;
using CharityRabbit.Models;
using System.Security.Claims;

namespace CharityRabbit.Api;

public static class OrganizationsEndpoints
{
    public static void MapOrganizationsApi(this IEndpointRouteBuilder api)
    {
        // ---- Public reads ----
        var pub = api.MapGroup("/organizations")
            .WithTags("Organizations")
            .AllowAnonymous()
            .AddEndpointFilter<OptionalBearerAuthFilter>();

        pub.MapGet("/", async (int? page, int? pageSize, string? search, IOrganizationService organizations) =>
        {
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);
            var items = await organizations.GetOrganizationsAsync((p - 1) * ps, ps, search);
            var total = p == 1 && items.Count < ps ? items.Count : await organizations.CountOrganizationsAsync(search);
            return new PagedResult<OrganizationModel>(items, total, p, ps);
        }).WithName("GetOrganizations");

        pub.MapGet("/{slug}", async (string slug, ClaimsPrincipal user, IOrganizationService organizations) =>
            await organizations.GetOrganizationBySlugAsync(slug, user.GetUserIdOrNull()) is { } org
                ? Results.Ok(org)
                : Results.NotFound())
            .WithName("GetOrganizationBySlug");

        pub.MapGet("/{id:long}/events", async (long id, IGoodWorksService goodWorks) =>
            await goodWorks.GetOrganizationEventsAsync(id))
            .WithName("GetOrganizationEvents");

        // ---- Authenticated (identity from token; org-mutating endpoints gate on admin —
        //      the service layer has no authorization of its own) ----
        var auth = api.MapGroup("/organizations")
            .WithTags("Organizations")
            .RequireAuthorization("Api");

        auth.MapPost("/", async (OrganizationModel model, ClaimsPrincipal user, IOrganizationService organizations) =>
        {
            var sub = user.GetRequiredUserId();
            model.Id = null;
            model.CreatedBy = sub;
            // Server-controlled fields: a client slug would bypass format/uniqueness rules
            // (empty forces generation), and verification is never client-grantable.
            model.Slug = string.Empty;
            model.IsVerified = false;
            var created = await organizations.CreateOrganizationAsync(model, sub);
            return Results.Created($"/api/v1/organizations/{created.Slug}", created);
        }).WithName("CreateOrganization");

        auth.MapPut("/{id:long}", async (long id, OrganizationModel model, ClaimsPrincipal user, IOrganizationService organizations) =>
        {
            if (await RequireAdminAsync(organizations, id, user) is { } error) return error;
            model.Id = id; // pin to the route — the body must not pick a different victim org
            return await organizations.UpdateOrganizationAsync(model) ? Results.NoContent() : Results.NotFound();
        }).WithName("UpdateOrganization");

        auth.MapDelete("/{id:long}", async (long id, ClaimsPrincipal user, IOrganizationService organizations) =>
        {
            if (await RequireAdminAsync(organizations, id, user) is { } error) return error;
            return await organizations.DeleteOrganizationAsync(id) ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteOrganization");

        // Member list carries emails/phones (PII): admins only.
        auth.MapGet("/{id:long}/members", async (long id, ClaimsPrincipal user, IOrganizationService organizations) =>
        {
            if (await RequireAdminAsync(organizations, id, user) is { } error) return error;
            return Results.Ok(await organizations.GetOrganizationMembersAsync(id));
        }).WithName("GetOrganizationMembers");

        auth.MapPost("/{id:long}/join", async (long id, ClaimsPrincipal user, IOrganizationService organizations) =>
            await organizations.AddMemberAsync(id, user.GetRequiredUserId())
                ? Results.NoContent()
                : Results.Conflict())
            .WithName("JoinOrganization");

        auth.MapDelete("/{id:long}/membership", async (long id, ClaimsPrincipal user, IOrganizationService organizations) =>
            await organizations.RemoveMemberAsync(id, user.GetRequiredUserId())
                ? Results.NoContent()
                : Results.NotFound())
            .WithName("LeaveOrganization");

        auth.MapPost("/{id:long}/members", async (long id, AddMemberRequest request, ClaimsPrincipal user, IOrganizationService organizations) =>
        {
            if (await RequireAdminAsync(organizations, id, user) is { } error) return error;
            // Role is always Member here; promotion is a separate, admin-gated action.
            return await organizations.AddMemberAsync(id, request.UserId) ? Results.NoContent() : Results.Conflict();
        }).WithName("AddOrganizationMember");

        auth.MapDelete("/{id:long}/members/{userId}", async (long id, string userId, ClaimsPrincipal user, IOrganizationService organizations) =>
        {
            var sub = user.GetRequiredUserId();
            // Admins may remove anyone; anyone may remove themselves.
            if (userId != sub && !await organizations.IsUserAdminAsync(id, sub)) return ApiResults.Forbidden();
            return await organizations.RemoveMemberAsync(id, userId) ? Results.NoContent() : Results.NotFound();
        }).WithName("RemoveOrganizationMember");

        auth.MapPost("/{id:long}/members/{userId}/promote", async (long id, string userId, ClaimsPrincipal user, IOrganizationService organizations) =>
        {
            if (await RequireAdminAsync(organizations, id, user) is { } error) return error;
            return await organizations.PromoteToAdminAsync(id, userId) ? Results.NoContent() : Results.NotFound();
        }).WithName("PromoteOrganizationMember");

        auth.MapGet("/slug-available", async (string slug, long? excludeId, IOrganizationService organizations) =>
            new { Available = await organizations.IsSlugAvailableAsync(slug, excludeId) })
            .WithName("IsSlugAvailable");
    }

    /// <summary>403 unless the caller administers the organization — the single gate for
    /// every org-mutating/PII endpoint (the service layer has no authorization of its own).</summary>
    private static async Task<IResult?> RequireAdminAsync(IOrganizationService organizations, long orgId, ClaimsPrincipal user) =>
        await organizations.IsUserAdminAsync(orgId, user.GetRequiredUserId()) ? null : ApiResults.Forbidden();
}
