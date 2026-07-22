using CharityRabbit.Platform;

namespace CharityRabbit.Maui.Services;

/// <summary>Native geolocation via MAUI Essentials (avoids WebView geolocation permission plumbing).</summary>
public class MauiGeolocationProvider : IGeolocationProvider
{
    // A cached fix older than this isn't worth showing as "your location".
    private static readonly TimeSpan LastKnownMaxAge = TimeSpan.FromMinutes(10);

    public async Task<GeoPosition?> GetCurrentPositionAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return null;

            // Try the cached fix first: GetLocationAsync frequently returns null on the first
            // call while iOS is still acquiring GPS, and it's near-instant when a recent fix
            // exists. Fall through to a live request if it's missing or stale.
            try
            {
                var cached = await Geolocation.Default.GetLastKnownLocationAsync();
                if (cached is not null
                    && DateTimeOffset.UtcNow - cached.Timestamp <= LastKnownMaxAge)
                {
                    return ToPosition(cached);
                }
            }
            catch (Exception)
            {
                // Cached lookup is best-effort; fall through to the live request.
            }

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            if (location is not null) return ToPosition(location);

            // Live request timed out (common indoors). Accept any cached fix at this point —
            // a stale position still beats dropping the user back to the nationwide view.
            try
            {
                var fallback = await Geolocation.Default.GetLastKnownLocationAsync();
                if (fallback is not null) return ToPosition(fallback);
            }
            catch (Exception)
            {
                // Ignore: nothing left to try.
            }

            return null;
        }
        catch (Exception)
        {
            return null; // Same degraded contract as the web provider: null = unavailable.
        }
    }

    private static GeoPosition ToPosition(Location l) =>
        new(l.Latitude, l.Longitude, l.Accuracy ?? 0);
}

public class MauiShareProvider : IShareProvider
{
    public async Task<ShareOutcome> ShareAsync(string title, string text, string url)
    {
        try
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = title,
                Text = text,
                Uri = url,
            });
            return ShareOutcome.Shared;
        }
        catch (Exception)
        {
            return ShareOutcome.Failed;
        }
    }
}

public class MauiAppEnvironment : IAppEnvironment
{
    public bool IsDevelopment()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
