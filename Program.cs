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
using Microsoft.Extensions.Options;
using MLS.Api.Services;
using MudBlazor.Services;
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

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapGroup("/authentication").MapLoginAndLogout();

        // Initialize predefined skills and database indexes on startup
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CharityDbContext>();
            db.Database.Migrate();

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
