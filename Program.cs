using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using CharityRabbit.Components;
using CharityRabbit.Components.Account;
using CharityRabbit.Data;
using Microsoft.Extensions.Options;
using GoogleMapsComponents;
using MLS.Api.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.Cookies;

internal class Program
{
    protected Program() { }

    private async static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpContextAccessor();

        builder.Services.Configure<Neo4jSettings>(builder.Configuration.GetSection("Neo4jSettings"));

        builder.Services.AddSingleton<Neo4jService>(sp =>
        {
            var neo4jSettings = sp.GetRequiredService<IOptions<Neo4jSettings>>().Value;
            var locationServices = sp.GetRequiredService<GeocodingService>();
            return new Neo4jService(neo4jSettings.Uri, neo4jSettings.Username, neo4jSettings.Password, locationServices);
        });

        builder.Services.AddHttpClient<GeocodingService>();
        builder.Services.AddSingleton<GooglePlacesService>();

        var googleMapsApiKey = builder.Configuration["GoogleMaps:ApiKey"] ?? throw new InvalidOperationException("GoogleMaps API key is missing in configuration.");

        builder.Services.AddBlazorGoogleMaps(googleMapsApiKey);

        builder.Services.AddMudServices();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();


        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, OidcAuthenticationStateProvider>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie("Cookies")
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = builder.Configuration["Oidc:Authority"];
            options.ClientId = builder.Configuration["Oidc:ClientId"];
            options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
            options.ResponseType = "code";
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                NameClaimType = "name",
            };
        });

        builder.Services.ConfigureCookieOidcRefresh(CookieAuthenticationDefaults.AuthenticationScheme, "oidc");


        builder.Services.AddAuthorization();

        builder.Services.AddCascadingAuthenticationState();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        // Handle reverse proxies if needed
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.All
        });

        app.UseStaticFiles();
        app.UseRouting();

        app.MapStaticAssets();
        app.UseAntiforgery();


        // Force HTTPS scheme to ensure correct OIDC redirects
        app.Use((context, next) =>
        {
            context.Request.Scheme = "https";
            return next();
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapGroup("/authentication").MapLoginAndLogout();


        await app.RunAsync();
    }
}
