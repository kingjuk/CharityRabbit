using GoogleMapsComponents;

namespace CharityRabbit.Data
{
    using GoogleApi;
    using GoogleApi.Entities.Places.AutoComplete.Request;
    using GoogleApi.Entities.Places.AutoComplete.Response;
    using GoogleMapsComponents;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class GooglePlacesService
    {
        private readonly string _apiKey;

        public GooglePlacesService(IConfiguration configuration)
        {
            _apiKey = configuration["GoogleMaps:ApiKey"] ?? throw new ArgumentNullException("Google API key not configured."); ;
        }

        public async Task<List<string>> GetAddressSuggestionsAsync(string input)
        {
            if(input == null || input.Length < 3)
            {
                return new List<string>();
            }

            var request = new PlacesAutoCompleteRequest
            {
                Key = _apiKey,
                Input = input
            };

            var response = await GooglePlaces.AutoComplete.QueryAsync(request);

            if (response.Status == GoogleApi.Entities.Common.Enums.Status.Ok)
            {
                var suggestions = new List<string>();
                foreach (var prediction in response.Predictions)
                {
                    suggestions.Add(prediction.Description);
                }
                return suggestions;
            }

            return new List<string>();
        }
    }

}
