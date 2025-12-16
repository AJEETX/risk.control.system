using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IWeatherInfoService
    {
        Task<string> GetWeatherAsync(string latitude, string longitude);
    }
    internal class WeatherInfoService : IWeatherInfoService
    {
        public async Task<string> GetWeatherAsync(string latitude, string longitude)
        {
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            using var httpClient = new HttpClient();
            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            string weatherCustomData = $"Temperature: {weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                                      $"\r\nWindspeed: {weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                                      $"\r\nElevation(sea level): {weatherData.elevation} metres";
            return weatherCustomData;
        }
    }
}
