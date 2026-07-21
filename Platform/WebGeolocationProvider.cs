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
    }
}
