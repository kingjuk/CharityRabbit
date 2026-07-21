using Microsoft.JSInterop;

namespace CharityRabbit.Platform;

/// <summary>Browser geolocation via wwwroot/js/geolocation.js.</summary>
public class WebGeolocationProvider(IJSRuntime js) : IGeolocationProvider
{
    public async Task<GeoPosition?> GetCurrentPositionAsync()
    {
        try
        {
            return await js.InvokeAsync<GeoPosition?>("geolocationHelper.getCurrentPosition");
        }
        catch (JSException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            // Thrown when called during prerendering ("component is being statically
            // rendered"). Callers should only request location from a user gesture, but
            // degrade instead of taking the page down if one slips through.
            return null;
        }
    }
}
