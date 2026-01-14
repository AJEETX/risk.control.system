using System.Text.Json;
using System.Text.RegularExpressions;

using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IPhoneService
    {
        Task<PhoneNumberInfo?> ValidateAsync(string mobileNumber);
        bool IsValidMobileNumber(string phoneNumber, string countryCode = "91");
    }

    internal class PhoneService : IPhoneService
    {
        private static readonly Dictionary<string, string> CountryPatterns = new()
        {
            // India: +91 / 91 / starts with 6-9 and 10 digits
            { "91", @"^(?:\+91|91)?[6-9]\d{9}$" },

            // USA / Canada: +1 / 1 / 10 digits (2-9 start)
            { "1", @"^(?:\+1|1)?[2-9]\d{9}$" },

            // UK: +44 / 44 / starts with 7 and 10 digits (mobile)
            { "44", @"^(?:\+44|44)?7\d{9}$" },

            // Australia: +61 / 61 / starts with 4 and 9 digits
            { "61", @"^(?:\+61|61)?4\d{8}$" },

            // UAE: +971 / 971 / starts with 5 and 9 digits
            { "971", @"^(?:\+971|971)?5\d{8}$" },

            // Add more as needed
        };
        private readonly IHttpClientFactory httpClientFactory;

        // Ideally move these to configuration or secrets manager
        private const string ApiHost = "phonenumbervalidatefree.p.rapidapi.com";
        private static string ApiKey = Environment.GetEnvironmentVariable("PHONE_API");
        private const string BaseUrl = $"https://{ApiHost}/ts_PhoneNumberValidateTest.jsp";
        public PhoneService(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }
        public async Task<PhoneNumberInfo?> ValidateAsync(string mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
                throw new ArgumentException("Mobile number must not be empty.", nameof(mobileNumber));

            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ArgumentException("ApiKey must not be empty.", nameof(ApiKey));

            // Sanitize phone number
            var encodedNumber = Uri.EscapeDataString($"+{mobileNumber}");

            var requestUrl = $"{BaseUrl}?number={encodedNumber}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            request.Headers.Add("x-rapidapi-key", ApiKey);
            request.Headers.Add("x-rapidapi-host", ApiHost);

            try
            {
                var httpClient = httpClientFactory.CreateClient();
                using var response = await httpClient.SendAsync(request);
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

        public bool IsValidMobileNumber(string phoneNumber, string countryCode = "91")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Normalize (remove spaces, dashes)
            phoneNumber = phoneNumber.Trim().TrimStart('0').Replace(" ", "").Replace("-", "");

            if (CountryPatterns.TryGetValue(countryCode, out var pattern))
            {
                return Regex.IsMatch(phoneNumber, pattern);
            }

            // Fallback: If no country pattern found, use a general E.164 check
            return Regex.IsMatch(phoneNumber, @"^\+?[1-9]\d{6,14}$");
        }
    }
}
