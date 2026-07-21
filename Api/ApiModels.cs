namespace CharityRabbit.Api;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

/// <summary>Toggle body for interest/signup: true to set, false to clear.</summary>
public record EngagementRequest(bool Value);

/// <summary>Admin adding a member. Role is intentionally absent — members always join as
/// "Member"; the promote endpoint is the only path to Admin.</summary>
public record AddMemberRequest(string UserId);

public record SetOrganizationRequest(long OrganizationId);

public record MeResponse(string UserId, string? Name, string? Email);

public static class ApiResults
{
    /// <summary>
    /// 403 via the Bearer scheme. A bare Results.Forbid() falls through to the default
    /// challenge scheme (oidc → cookie handler), which 302-redirects API clients to an
    /// HTML access-denied page instead of returning a status code.
    /// </summary>
    public static IResult Forbidden() => Results.Forbid(authenticationSchemes: ["Bearer"]);
}
