using CharityRabbit.Platform;

namespace CharityRabbit.Maui.Services;

/// <summary>Native geolocation via MAUI Essentials (avoids WebView geolocation permission plumbing).</summary>
public class MauiGeolocationProvider : IGeolocationProvider
{
    public async Task<GeoPosition?> GetCurrentPositionAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return null;

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            return location is null ? null : new GeoPosition(location.Latitude, location.Longitude, location.Accuracy ?? 0);
        }
        catch (Exception)
        {
            return null; // Same degraded contract as the web provider: null = unavailable.
        }
    }
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
