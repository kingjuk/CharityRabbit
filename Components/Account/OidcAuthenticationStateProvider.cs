using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;


namespace CharityRabbit.Components.Account;

public class OidcAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OidcAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(user ?? new ClaimsPrincipal())));
    }
}

