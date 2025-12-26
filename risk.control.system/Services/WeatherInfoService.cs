using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IWeatherInfoService
    {
        Task<string> GetWeatherAsync(string latitude, string longitude);
    }
    internal class WeatherInfoService : IWeatherInfoService
    {
        private readonly IHttpClientFactory httpClientFctory;

        public WeatherInfoService(IHttpClientFactory httpClientFctory)
        {
            this.httpClientFctory = httpClientFctory;
        }
        public async Task<string> GetWeatherAsync(string latitude, string longitude)
        {
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m";

            // Best practice: Use a static client or IHttpClientFactory
            using var request = new HttpRequestMessage(HttpMethod.Get, weatherUrl);
            request.Headers.Add("User-Agent", "RiskControlSystem-WeatherClient");

            try
            {
                var httpClient = httpClientFctory.CreateClient();
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode(); // Throws if the server returns 4xx or 5xx

                var weatherData = await response.Content.ReadFromJsonAsync<Weather>();

                return $"Temperature: {weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}. " +
                       $"\nWindspeed: {weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}";
            }
            catch (Exception ex)
            {
                // Log the specific inner exception details
                return $"Weather update failed: {ex.Message}";
            }
        }
    }
}
