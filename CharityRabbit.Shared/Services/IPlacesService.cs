namespace CharityRabbit.Data;

/// <summary>
/// Address autocomplete. Server implementation uses the Google Places API directly;
/// the mobile implementation calls the /api/v1/geo/places proxy.
/// </summary>
public interface IPlacesService
{
    Task<List<string>> GetAddressSuggestionsAsync(string input);
}
