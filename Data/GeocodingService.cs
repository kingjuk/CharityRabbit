using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text.Json;

namespace MLS.Api.Services
{
    public class GeocodingService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeocodingService(IConfiguration configuration, HttpClient httpClient)
        {
            _apiKey = configuration["GoogleMaps:ApiKey"] ?? throw new ArgumentNullException("Google API key not configured."); ;
            _httpClient = httpClient;
        }

        public async Task<(double Latitude, double Longitude)> GetCoordinatesAsync(string address)
        {
            string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={_apiKey}";

            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(jsonResponse);

                if (json["status"]?.ToString() == "OK")
                {
                    var location = json["results"][0]["geometry"]["location"];
                    double lat = (double)location["lat"];
                    double lng = (double)location["lng"];
                    return (lat, lng);
                }
                else
                {
                    throw new Exception("Unable to geocode the address. Status: " + json["status"]);
                }
            }
            else
            {
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
            }
        }

        public async Task<(string city, string state, string country, string zip)> GetLocationDetailsAsync(double lat, double lng)
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&key={_apiKey}";
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
