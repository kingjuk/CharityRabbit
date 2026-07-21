using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace CharityRabbit.Api;

/// <summary>
/// For anonymous-but-personalized read endpoints (search results carry IsUserInterested /
/// IsUserSignedUp flags). Pins the request's principal to the Bearer token if one is
/// presented, and to anonymous otherwise — API personalization never derives from the
/// site's auth cookie, so cookies can never authenticate an API call.
/// </summary>
public class OptionalBearerAuthFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var result = await http.AuthenticateAsync("Bearer");

        // A token was presented but failed validation (expired, wrong audience, …).
        // Answer 401 rather than silently serving unpersonalized data — the client needs
        // the signal to refresh its token.
        if (!result.Succeeded && http.Request.Headers.ContainsKey("Authorization"))
        {
            return Results.Challenge(authenticationSchemes: ["Bearer"]);
        }

        var principal = result.Succeeded ? result.Principal : new ClaimsPrincipal(new ClaimsIdentity());
        http.User = principal;

        // Minimal-API handler arguments are bound BEFORE endpoint filters run, so any
        // ClaimsPrincipal parameter already captured the pre-filter (cookie or anonymous)
        // principal — replace those too or the swap above never reaches the handler.
        for (var i = 0; i < context.Arguments.Count; i++)
        {
            if (context.Arguments[i] is ClaimsPrincipal)
            {
                context.Arguments[i] = principal;
            }
        }

        return await next(context);
    }
}
