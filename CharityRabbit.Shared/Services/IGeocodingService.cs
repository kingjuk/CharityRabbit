using CharityRabbit.Models;

namespace CharityRabbit.Data;

/// <summary>
/// Geocoding via Google APIs. Server implementation calls Google directly with the
/// server-side key; the mobile implementation calls the /api/v1/geo proxy so the key
/// never ships in the app.
/// </summary>
public interface IGeocodingService
{
    Task<GeoPoint> GetCoordinatesAsync(string address);
    Task<LocationDetails> GetLocationDetailsAsync(double lat, double lng);
}
