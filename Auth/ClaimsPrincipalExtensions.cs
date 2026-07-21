using System.Security.Claims;

namespace CharityRabbit.Auth;

/// <summary>
/// Single source of truth for reading identity off a principal, shared by the cookie/OIDC
/// path (Blazor) and the JWT bearer path (/api). Both handlers keep the default inbound
/// claim map, so Keycloak's 'sub' arrives as ClaimTypes.NameIdentifier on both — the raw
/// sub is the users-table primary key, so the two paths MUST agree or accounts silently
/// split. Keycloak (auth.kingjuk.com) includes name/email in access tokens via the
/// profile/email scopes, so no custom claim mappers are required.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal user) =>
        user.GetUserIdOrNull()
        ?? throw new InvalidOperationException("Authenticated principal has no subject claim.");

    public static string? GetUserIdOrNull(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
            ? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")
            : null;

    public static string? GetDisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name)
        ?? user.FindFirstValue("name")
        ?? user.FindFirstValue("preferred_username")
        ?? user.Identity?.Name;

    public static string? GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email)
        ?? user.FindFirstValue("email");
}
