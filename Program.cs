using CharityRabbit.Components;
using CharityRabbit.Components.Account;
using CharityRabbit.Data;
using CharityRabbit.Models;
using GoogleMapsComponents;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using CharityRabbit.Api;
using CharityRabbit.Auth;
using CharityRabbit.Platform;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using System.Threading.RateLimiting;
using Neo4j.Driver;
using Serilog;
using Serilog.Events;

internal class Program
{
    protected Program() { }

    private async static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration), false);

        // Health check
        builder.Services.AddHealthChecks();

        builder.Services.AddHttpContextAccessor();

        // Neo4jSettings retained only for the one-shot data-copy tool (see Neo4jToPostgresMigrator).
        builder.Services.Configure<Neo4jSettings>(builder.Configuration.GetSection("Neo4jSettings"));

        builder.Services.AddDbContext<CharityDbContext>(o =>
            o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

        // Persist data-protection keys in Postgres so every deploy doesn't invalidate
        // sessions and antiforgery tokens (keys were container-local before).
        builder.Services.AddDataProtection()
            .PersistKeysToDbContext<CharityDbContext>()
            .SetApplicationName("CharityRabbit");

        builder.Services.AddHttpClient<GeocodingService>();
        builder.Services.AddSingleton<GooglePlacesService>();
        builder.Services.AddScoped<GoodWorksService>();
        builder.Services.AddScoped<TestDataService>();
        builder.Services.AddScoped<RecurringEventService>();
        builder.Services.AddScoped<SeoService>();
        builder.Services.AddScoped<OrganizationService>();
        builder.Services.AddScoped<SkillService>();

        // Shared-interface registrations forward to the concrete EF/Google services above,
        // so components (which inject the interfaces, same as the future MAUI app) and
        // server-only callers (which keep the concretes) resolve the same implementations.
        // Geocoding is transient (typed-HttpClient lifetime) and stateless, so instances
        // aren't shared; the scoped services below are one instance per scope.
        builder.Services.AddTransient<IGeocodingService>(sp => sp.GetRequiredService<GeocodingService>());
        builder.Services.AddSingleton<IPlacesService>(sp => sp.GetRequiredService<GooglePlacesService>());
        builder.Services.AddScoped<IGoodWorksService>(sp => sp.GetRequiredService<GoodWorksService>());
        builder.Services.AddScoped<IOrganizationService>(sp => sp.GetRequiredService<OrganizationService>());
        builder.Services.AddScoped<ISkillService>(sp => sp.GetRequiredService<SkillService>());

        // Per-platform providers (browser JS interop here; MAUI Essentials in the mobile host).
        builder.Services.AddScoped<IGeolocationProvider, WebGeolocationProvider>();
        builder.Services.AddScoped<IShareProvider, WebShareProvider>();
        builder.Services.AddSingleton<IAppEnvironment, WebAppEnvironment>();
        builder.Services.AddSingleton(new NavMenuComponent(typeof(CharityRabbit.Components.Layout.NavMenu)));

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
            // Local dev runs Keycloak on plain http; defaults to true everywhere else.
            options.RequireHttpsMetadata = builder.Configuration.GetValue("Oidc:RequireHttpsMetadata", true);
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

        // Bearer scheme for /api/v1 (mobile clients). Non-default: the Blazor site keeps
        // cookie/oidc; API endpoints opt in via the "Api" policy below so auth failures
        // return 401 instead of a 302 redirect to the identity provider.
        builder.Services.AddAuthentication()
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = builder.Configuration["Oidc:Authority"];
                options.Audience = builder.Configuration["Oidc:Audience"];
                // Local dev runs Keycloak on plain http; defaults to true everywhere else.
                options.RequireHttpsMetadata = builder.Configuration.GetValue("Oidc:RequireHttpsMetadata", true);
                options.TokenValidationParameters.NameClaimType = "name";
                // Leave MapInboundClaims at its default (true) even though many samples
                // disable it: 'sub' must arrive as ClaimTypes.NameIdentifier exactly like
                // the cookie path does, or the same person gets a second users row.
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Api", policy => policy
                .AddAuthenticationSchemes("Bearer")
                .RequireAuthenticatedUser());

        // Partition by token subject on policy-authenticated endpoints (the authorization
        // middleware authenticates Bearer before the limiter runs). Anonymous endpoints fall
        // back to the client IP: behind the proxy RemoteIpAddress is the proxy itself
        // (KnownProxies isn't configured — same reason the forced-https middleware exists),
        // so prefer the first X-Forwarded-For hop. Spoofable, but fine for rate limiting.
        static string RateLimitClientKey(HttpContext context) =>
            context.User.GetUserIdOrNull()
            ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("api", context => RateLimitPartition.GetTokenBucketLimiter(
                RateLimitClientKey(context),
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 100,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    AutoReplenishment = true,
                    QueueLimit = 0,
                }));
            // Stricter cap on the Google proxies — they cost money per call.
            options.AddPolicy("geo", context => RateLimitPartition.GetFixedWindowLimiter(
                RateLimitClientKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        builder.Services.AddOpenApi();

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

        // Configure request logging to use Debug level
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                // Attempt to get the remote IP address and add it to the diagnostic context
                var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
                diagnosticContext.Set("ClientIP", remoteIpAddress?.ToString());
            };
            options.GetLevel = (context, _, ex) =>
                context.Response.StatusCode >= StatusCodes.Status400BadRequest || ex != null
                    ? LogEventLevel.Warning
                    : LogEventLevel.Debug;
            options.IncludeQueryInRequestPath = true;
        });

        app.UseAuthentication();
        app.UseAuthorization();
        // After auth so the API limiter can partition by token subject rather than IP.
        app.UseRateLimiter();

        app.MapStaticAssets();
        // AddAdditionalAssemblies here registers SSR endpoints for the pages that moved to
        // the Shared RCL — Routes.razor's Router parameter only covers interactive routing,
        // so without this every direct navigation (including "/") would 404.
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(CharityRabbit.Components.Layout.MainLayout).Assembly);

        app.MapGroup("/authentication").MapLoginAndLogout();

        app.MapHealthChecks("/health").AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi(); // /openapi/v1.json
        }

        // REST API consumed by the mobile app (bearer auth; see the "Api" policy).
        var api = app.MapGroup("/api/v1").RequireRateLimiting("api");
        api.MapGoodWorksApi();
        api.MapOrganizationsApi();
        api.MapSkillsApi();
        api.MapMeApi();
        api.MapGeoApi();

        // Initialize predefined skills and database indexes on startup
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CharityDbContext>();
            db.Database.Migrate();

            if (args.Contains("seed-test-data"))
            {
                var testData = scope.ServiceProvider.GetRequiredService<TestDataService>();
                var imported = await testData.ImportTestDataAsync();
                Console.WriteLine($"Imported {imported} test good works.");
                return;
            }

            if (args.Contains("migrate-neo4j"))
            {
                var settings = scope.ServiceProvider.GetRequiredService<IOptions<Neo4jSettings>>().Value;
                await Neo4jToPostgresMigrator.RunAsync(settings, db);
                Console.WriteLine("neo4j → Postgres copy complete.");
                return;
            }

            try
            {
                var skillService = scope.ServiceProvider.GetRequiredService<SkillService>();
                await skillService.InitializePredefinedSkillsAsync();
                Console.WriteLine("Predefined skills initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not initialize database resources: {ex.Message}");
            }
        }

        // SEO: Sitemap endpoint
        app.MapGet("/sitemap.xml", async (GoodWorksService neo4jService, SeoService seoService) =>
        {
            var goodWorks = await neo4jService.SearchGoodWorksAsync(new GoodWorksSearchCriteria(), null);
            var sitemap = await seoService.GenerateSitemapXml(goodWorks);
            return Results.Content(sitemap, "application/xml");
        });

        await app.RunAsync();
    }
}
