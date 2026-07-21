namespace CharityRabbit.Platform;

/// <summary>
/// The nav menu is per-host (web logs out via a form POST to the cookie endpoint; the
/// MAUI host will use token login/logout), so the shared MainLayout renders whichever
/// component type the host registers.
/// </summary>
public sealed record NavMenuComponent(Type ComponentType);
