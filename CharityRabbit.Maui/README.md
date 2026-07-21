# CharityRabbit Mobile (.NET MAUI Blazor Hybrid)

iOS/iPadOS app that reuses the shared Razor components (`CharityRabbit.Shared`)
in a `BlazorWebView` and talks to the `/api/v1` REST API. Android is scaffolded
in the csproj (`net10.0-android`, currently commented out) and can be re-enabled
once the Android SDK is set up.

## Architecture

- **Auth** — Keycloak PKCE via `IdentityModel.OidcClient` + MAUI `WebAuthenticator`
  (`Services/AuthService.cs`, also the `AuthenticationStateProvider`). Refresh token
  in `SecureStorage`; access token in memory. `AccessTokenHandler` attaches the bearer
  and does one refresh-and-retry on 401.
- **Data** — `Services/ApiClients.cs` implement the shared `IGoodWorksService` /
  `IOrganizationService` / `ISkillService` / `IGeocodingService` / `IPlacesService`
  interfaces over `System.Net.Http.Json`. Identity always comes from the token, never
  the client.
- **Platform** — `Services/PlatformProviders.cs`: native geolocation/share/env.
- **Endpoints** — `Services/AppConfig.cs`. DEBUG → `http://localhost:5222` (API) and
  `http://localhost:8081` (Keycloak dev). RELEASE → `https://charityrabbit.com` and
  `https://auth.kingjuk.com`.

## Verified in the iOS simulator (this session)

Home, Search (card list + collapsible filters), Detail, Organizations, Profile,
Keycloak PKCE login end-to-end, authenticated sign-up (write path), session restore
from SecureStorage, and a **Release/trimmed build deserializing live API data**
(the `JsonSerializerIsReflectionEnabledByDefault` guard is what keeps that working).

## Remaining human-only steps before App Store submission

1. **Apple Developer account** ($99/yr) + an App ID for `com.jdmsolutions.charityrabbit`,
   a distribution certificate, and a provisioning profile. Device/`ipa` Release builds
   require signing (simulator builds do not).
2. **App Store Connect**: create the app record, screenshots (the simulator captures
   from this session are a starting point), description, privacy questionnaire
   (the app collects account email/name and location — declare accordingly).
3. **Keycloak (production realm `CharityRabbit` at auth.kingjuk.com)** — one-time:
   - Client `charityrabbit-api` (audience only) + a client scope with an **Audience
     mapper** emitting `charityrabbit-api`.
   - Public PKCE client `charityrabbit-mobile`, redirect `charityrabbit://callback`,
     `offline_access`, refresh-token rotation on.
   (These already exist in the *local dev* Keycloak used for testing.)
4. **Deploy the API** — the `/api/v1` endpoints must be live on charityrabbit.com
   (they build but the running ECS task predates them) and `Oidc:Audience=charityrabbit-api`
   present in production config.
5. **Maps** — the map tab needs a Google Maps-JavaScript key restricted to the WebView
   origin (`https://0.0.0.0/*`); currently a placeholder, so the map degrades but list
   views work. Add before enabling the map on mobile.
6. **Android** — re-enable `net10.0-android` TFM, set up the Android SDK, add the
   `charityrabbit://callback` intent filter, and repeat store setup for Google Play ($25).

## Running locally (for the next dev session)

The dev backend used this session: local Postgres (`charityrabbit` db, migrations +
`dotnet run -- seed-test-data`), the web app on `http://localhost:5222`
(`ASPNETCORE_URLS=http://localhost:5222 dotnet run`), and a local Keycloak dev server
on `:8081` with realm `CharityRabbit`, client `charityrabbit-mobile`, and a test user.
Build/run the app: `dotnet build CharityRabbit.Maui -f net10.0-ios` then
`xcrun simctl install booted <app>` / `xcrun simctl launch booted com.jdmsolutions.charityrabbit`.
