namespace Preservation.API
{
    public interface IPreservation
    {
        Task<WeatherForecast[]> GetWeatherForecasts();
    }
}
