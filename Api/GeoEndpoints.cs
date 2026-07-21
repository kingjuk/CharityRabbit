using CharityRabbit.Data;

namespace CharityRabbit.Api;

/// <summary>
/// Proxies for the Google Geocoding/Places APIs so the server-side Google key never
/// ships in a client. Bearer-authenticated and tightly rate-limited: these cost money.
/// </summary>
public static class GeoEndpoints
{
    public static void MapGeoApi(this IEndpointRouteBuilder api)
    {
        var geo = api.MapGroup("/geo")
            .WithTags("Geo")
            .RequireAuthorization("Api")
            .RequireRateLimiting("geo");

        geo.MapGet("/geocode", async (string address, IGeocodingService geocoding) =>
        {
            try
            {
                return Results.Ok(await geocoding.GetCoordinatesAsync(address));
            }
            catch (HttpRequestException)
            {
                return Results.StatusCode(StatusCodes.Status502BadGateway);
            }
            catch (Exception)
            {
                // The service throws the same Exception type for zero results and
                // Google-side status errors; treat both as not-found.
                return Results.NotFound();
            }
        }).WithName("Geocode");

        geo.MapGet("/reverse", async (double lat, double lng, IGeocodingService geocoding) =>
        {
            try
            {
                return Results.Ok(await geocoding.GetLocationDetailsAsync(lat, lng));
            }
            catch (HttpRequestException)
            {
                return Results.StatusCode(StatusCodes.Status502BadGateway);
            }
            catch (Exception)
            {
                return Results.NotFound();
            }
        }).WithName("ReverseGeocode");

        geo.MapGet("/places", async (string input, IPlacesService places) =>
            await places.GetAddressSuggestionsAsync(input))
            .WithName("GetAddressSuggestions");
    }
}
