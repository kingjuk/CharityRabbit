using CharityRabbit.Data;
using CharityRabbit.Maui.Services;
using CharityRabbit.Platform;
using GoogleMapsComponents;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace CharityRabbit.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();

        // Maps need a client-side Maps-JavaScript key (referrer-restricted; see plan Phase 4).
        // With the placeholder the map surfaces degrade; list views are unaffected.
        builder.Services.AddBlazorGoogleMaps("dev-placeholder-key");

        // ---- Auth (PKCE against Keycloak; AuthService is also the auth-state provider) ----
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthService>());

        // ---- API access: one HttpClient with the bearer/refresh handler ----
        builder.Services.AddSingleton(sp => new HttpClient(
            new AccessTokenHandler(sp.GetRequiredService<AuthService>()) { InnerHandler = new HttpClientHandler() })
        {
            BaseAddress = new Uri(AppConfig.ApiBase),
            Timeout = TimeSpan.FromSeconds(30),
        });

        // Shared data interfaces → HTTP implementations against /api/v1
        builder.Services.AddSingleton<IGoodWorksService, GoodWorksApiClient>();
        builder.Services.AddSingleton<IOrganizationService, OrganizationsApiClient>();
        builder.Services.AddSingleton<ISkillService, SkillsApiClient>();
        builder.Services.AddSingleton<GeoApiClient>();
        builder.Services.AddSingleton<IGeocodingService>(sp => sp.GetRequiredService<GeoApiClient>());
        builder.Services.AddSingleton<IPlacesService>(sp => sp.GetRequiredService<GeoApiClient>());

        // Pure shared services (run in-process, no server needed)
        builder.Services.AddSingleton<RecurringEventService>();
        builder.Services.AddSingleton<SeoService>();

        // Platform providers + per-host nav menu
        builder.Services.AddSingleton<IGeolocationProvider, MauiGeolocationProvider>();
        builder.Services.AddSingleton<IShareProvider, MauiShareProvider>();
        builder.Services.AddSingleton<IAppEnvironment, MauiAppEnvironment>();
        builder.Services.AddSingleton(new NavMenuComponent(typeof(Components.Layout.NavMenu)));

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Restore the previous session from the stored refresh token; the auth-state
        // provider notifies the UI whenever it completes.
        _ = app.Services.GetRequiredService<AuthService>().TryRestoreSessionAsync();

        return app;
    }
}
