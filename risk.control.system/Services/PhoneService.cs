using System.Text.Json;

using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IPhoneService
    {
        Task<PhoneNumberInfo?> ValidateAsync(string phoneNumber, string? country = null);
    }

    public class PhoneService : IPhoneService
    {
        private static readonly HttpClient _httpClient = new();

        // Ideally move these to configuration or secrets manager
        private const string ApiHost = "phonenumbervalidatefree.p.rapidapi.com";
        private static string ApiKey = Environment.GetEnvironmentVariable("PHONE_API");
        private const string BaseUrl = $"https://{ApiHost}/ts_PhoneNumberValidateTest.jsp";

        public async Task<PhoneNumberInfo?> ValidateAsync(string phoneNumber, string? country = null)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number must not be empty.", nameof(phoneNumber));

            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ArgumentException("ApiKey must not be empty.", nameof(ApiKey));

            // Sanitize phone number
            var encodedNumber = Uri.EscapeDataString($"+{phoneNumber}");

            var requestUrl = $"{BaseUrl}?number={encodedNumber}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            request.Headers.Add("x-rapidapi-key", ApiKey);
            request.Headers.Add("x-rapidapi-host", ApiHost);

            try
            {
                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<PhoneNumberInfo>(body, options);
                // Optional: log raw body for debugging
                Console.WriteLine($"[PhoneService] Response: {body}");

                return result;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"[PhoneService] HTTP error: {ex.Message}");
                throw new InvalidOperationException("Failed to validate phone number from API.", ex);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[PhoneService] JSON parse error: {ex.Message}");
                throw new InvalidOperationException("Failed to parse API response.", ex);
            }
        }
    }
}
