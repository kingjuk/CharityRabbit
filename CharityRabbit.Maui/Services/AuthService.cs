using IdentityModel.OidcClient;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace CharityRabbit.Maui.Services;

/// <summary>
/// Owns the whole mobile token lifecycle: PKCE login via the system browser, refresh-token
/// persistence in SecureStorage (access tokens stay in memory only), silent session restore
/// on launch, and serialized refresh so concurrent 401s can't burn a rotated refresh token.
/// Also acts as the app's AuthenticationStateProvider.
/// </summary>
public class AuthService : AuthenticationStateProvider
{
    private const string RefreshTokenKey = "cr_refresh_token";

    private readonly OidcClient _oidc;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private ClaimsPrincipal _principal = Anonymous;
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;

    private static ClaimsPrincipal Anonymous => new(new ClaimsIdentity());

    public AuthService()
    {
        var options = new OidcClientOptions
        {
            Authority = AppConfig.Authority,
            ClientId = AppConfig.ClientId,
            RedirectUri = AppConfig.RedirectUri,
            PostLogoutRedirectUri = AppConfig.RedirectUri,
            Scope = AppConfig.Scope,
            Browser = new WebAuthenticatorBrowser(),
        };
        options.Policy.Discovery.RequireHttps = AppConfig.RequireHttpsDiscovery;
        _oidc = new OidcClient(options);
    }

    public bool IsAuthenticated => _principal.Identity?.IsAuthenticated == true;

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(_principal));

    /// <summary>Interactive login via the system browser. Returns an error string, or null on success.</summary>
    public async Task<string?> LoginAsync()
    {
        var result = await _oidc.LoginAsync(new LoginRequest());
        if (result.IsError)
        {
            return result.Error == "UserCancel" ? null : result.Error;
        }

        await ApplySessionAsync(result.User.Claims, result.AccessToken, result.AccessTokenExpiration, result.RefreshToken);
        return null;
    }

    /// <summary>Silent restore on app launch from the stored refresh token.</summary>
    public async Task TryRestoreSessionAsync()
    {
        string? refreshToken;
        try
        {
            refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
        }
        catch (Exception)
        {
            return; // Keychain unavailable (fresh simulator, device lock) — stay logged out.
        }
        if (string.IsNullOrEmpty(refreshToken)) return;

        var result = await _oidc.RefreshTokenAsync(refreshToken);
        if (result.IsError)
        {
            await ClearSessionAsync();
            return;
        }

        // The refresh response has tokens but no user claims — fetch them.
        var userInfo = await _oidc.GetUserInfoAsync(result.AccessToken);
        var claims = userInfo.IsError ? [] : userInfo.Claims;
        await ApplySessionAsync(claims, result.AccessToken, result.AccessTokenExpiration, result.RefreshToken);
    }

    /// <summary>Current access token, refreshing if it expires within a minute. Null when logged out
    /// or refresh fails (session is cleared so the UI flips to logged-out).</summary>
    public async Task<string?> GetAccessTokenAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _accessToken is not null && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddSeconds(60))
        {
            return _accessToken;
        }

        await _refreshLock.WaitAsync();
        try
        {
            // Re-check: another caller may have refreshed while we waited.
            if (!forceRefresh && _accessToken is not null && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddSeconds(60))
            {
                return _accessToken;
            }

            var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            if (string.IsNullOrEmpty(refreshToken)) return _accessToken;

            var result = await _oidc.RefreshTokenAsync(refreshToken);
            if (result.IsError)
            {
                await ClearSessionAsync();
                return null;
            }

            _accessToken = result.AccessToken;
            _accessTokenExpiresAt = result.AccessTokenExpiration;
            // Persist the rotated refresh token immediately — the old one may now be dead.
            if (!string.IsNullOrEmpty(result.RefreshToken))
            {
                await SecureStorage.Default.SetAsync(RefreshTokenKey, result.RefreshToken);
            }
            return _accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task LogoutAsync()
    {
        await ClearSessionAsync();
    }

    private async Task ApplySessionAsync(IEnumerable<Claim> claims, string accessToken, DateTimeOffset expiresAt, string? refreshToken)
    {
        _accessToken = accessToken;
        _accessTokenExpiresAt = expiresAt;
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        }

        _principal = BuildPrincipal(claims);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_principal)));
    }

    private async Task ClearSessionAsync()
    {
        _accessToken = null;
        _accessTokenExpiresAt = DateTimeOffset.MinValue;
        SecureStorage.Default.Remove(RefreshTokenKey);
        _principal = Anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_principal)));
        await Task.CompletedTask;
    }

    /// <summary>Mirror the server's claim mapping: 'sub' must surface as ClaimTypes.NameIdentifier
    /// (it is the users-table key) and name/email under both raw and mapped types.</summary>
    private static ClaimsPrincipal BuildPrincipal(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(authenticationType: "oidc", nameType: "name", roleType: "role");
        foreach (var claim in claims)
        {
            identity.AddClaim(new Claim(claim.Type, claim.Value));
            switch (claim.Type)
            {
                case "sub":
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, claim.Value));
                    break;
                case "email":
                    identity.AddClaim(new Claim(ClaimTypes.Email, claim.Value));
                    break;
                case "name":
                    identity.AddClaim(new Claim(ClaimTypes.Name, claim.Value));
                    break;
            }
        }
        return new ClaimsPrincipal(identity);
    }
}
