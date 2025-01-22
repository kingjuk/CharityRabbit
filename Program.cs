using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using CharityRabbit.Components;
using CharityRabbit.Components.Account;
using CharityRabbit.Data;
using Microsoft.Extensions.Options;
using GoogleMapsComponents;
using MLS.Api.Services;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    protected Program()
    {

    }


    private async static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpContextAccessor();

        // Configure Neo4jSettings from appsettings.json
        builder.Services.Configure<Neo4jSettings>(builder.Configuration.GetSection("Neo4jSettings"));

        // Register Neo4jService using Neo4jSettings
        builder.Services.AddSingleton<Neo4jService>(sp =>
        {
            var neo4jSettings = sp.GetRequiredService<IOptions<Neo4jSettings>>().Value;
            var locationServices = sp.GetRequiredService<GeocodingService>();
            return new Neo4jService(neo4jSettings.Uri, neo4jSettings.Username, neo4jSettings.Password, locationServices);
        });

        builder.Services.AddHttpClient<GeocodingService>();
        builder.Services.AddSingleton<GooglePlacesService>();


        var googleMapsApiKey = builder.Configuration["GoogleMaps:ApiKey"];

        if (googleMapsApiKey == null)
        {
            throw new InvalidOperationException("GoogleMaps:ApiKey is not set in appsettings.json");
        }
        else
        {
            builder.Services.AddBlazorGoogleMaps(googleMapsApiKey);
        }


        // Add MudBlazor services
        builder.Services.AddMudServices();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, OidcAuthenticationStateProvider>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "oidc";  // Set OIDC as the challenge scheme
        })
        .AddCookie("Cookies")
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = builder.Configuration["Oidc:Authority"]; // OIDC provider URL (e.g., Azure AD, Google)
            options.ClientId = builder.Configuration["Oidc:ClientId"]; // Your client ID
            options.ClientSecret = builder.Configuration["Oidc:ClientSecret"]; // Your client secret
            options.ResponseType = "code";  // Authorization Code flow
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                NameClaimType = "name",  // Use 'name' claim instead of default 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'
            };
        });

        builder.Services.AddAuthorization();

        // builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAntiforgery();

        //force scheme to https so redirect to login works
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

        await app.RunAsync();
    }
}