namespace CharityRabbit.Platform;

public record GeoPosition(double Latitude, double Longitude, double Accuracy);

/// <summary>
/// Current-device location. Web implementation uses the browser Geolocation API via JS
/// interop; the MAUI implementation uses platform geolocation (MAUI Essentials).
/// </summary>
public interface IGeolocationProvider
{
    /// <returns>The current position, or null if unavailable or permission was denied.</returns>
    Task<GeoPosition?> GetCurrentPositionAsync();
}
