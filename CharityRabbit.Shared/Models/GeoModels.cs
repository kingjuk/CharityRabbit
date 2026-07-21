namespace CharityRabbit.Models;

/// <summary>Latitude/longitude pair. Positional record so existing tuple-style
/// deconstruction (<c>var (lat, lng) = ...</c>) keeps working and it round-trips JSON.</summary>
public record GeoPoint(double Latitude, double Longitude);

/// <summary>Reverse-geocoding result.</summary>
public record LocationDetails(string City, string State, string Country, string Zip);
