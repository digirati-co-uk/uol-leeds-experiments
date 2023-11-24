using System.Net.Http.Json;

namespace Preservation.API
{
    public class PreservationService : IPreservation
    {
        private readonly HttpClient _httpClient;

        public PreservationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherForecast[]> GetWeatherForecasts()
        {
            var response = await _httpClient.GetAsync("/WeatherForecast");
            var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
            if(forecasts != null)
            {
                return forecasts;
            }
            return Array.Empty<WeatherForecast>();
        }
    }
}
