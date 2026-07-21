namespace CharityRabbit.Maui.Services;

/// <summary>
/// Environment endpoints. DEBUG points at the local dev stack (API on localhost:5222,
/// Keycloak dev realm on localhost:8081 — the iOS simulator shares the host loopback);
/// RELEASE points at production.
/// </summary>
public static class AppConfig
{
#if DEBUG
    public const string ApiBase = "http://localhost:5222";
    public const string Authority = "http://localhost:8081/realms/CharityRabbit";
    public const bool RequireHttpsDiscovery = false;
#else
    public const string ApiBase = "https://charityrabbit.com";
    public const string Authority = "https://auth.kingjuk.com/realms/CharityRabbit";
    public const bool RequireHttpsDiscovery = true;
#endif

    public const string ClientId = "charityrabbit-mobile";
    public const string RedirectUri = "charityrabbit://callback";
    public const string Scope = "openid profile email offline_access";
}
