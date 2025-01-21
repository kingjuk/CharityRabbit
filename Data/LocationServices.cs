using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CharityRabbit.Data
{
    public class LocationServices
    {
        private readonly HttpClient _httpClient;
        private readonly string _googleApiKey;

        public LocationServices(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _googleApiKey = configuration["GoogleMaps:ApiKey"] ?? throw new ArgumentNullException("Google API key not configured.");
        }

        public async Task<(string city, string state, string country, string zip)> GetLocationDetailsAsync(double lat, double lng)
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&key={_googleApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve location data. Status: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);

            if (json.GetProperty("status").GetString() != "OK")
            {
                throw new Exception("Google Maps API failed to retrieve valid location data.");
            }

            var results = json.GetProperty("results");

            string city = "Unknown";
            string state = "Unknown";
            string country = "Unknown";
            string zip = "Unknown";

            foreach (var result in results.EnumerateArray())
            {
                var addressComponents = result.GetProperty("address_components");

                city = GetComponent(addressComponents, "locality") ?? city;
                state = GetComponent(addressComponents, "administrative_area_level_1") ?? state;
                country = GetComponent(addressComponents, "country") ?? country;
                zip = GetComponent(addressComponents, "postal_code") ?? zip;

                if (city != "Unknown" && state != "Unknown" && country != "Unknown" && zip != "Unknown")
                {
                    break;  // Exit once all values are found
                }
            }

            return (city, state, country, zip);
        }

        private string GetComponent(JsonElement addressComponents, string type)
        {
            foreach (var component in addressComponents.EnumerateArray())
            {
                foreach (var componentType in component.GetProperty("types").EnumerateArray())
                {
                    if (componentType.GetString() == type)
                    {
                        return component.GetProperty("long_name").GetString();
                    }
                }
            }
            return null;
        }
    }
}
