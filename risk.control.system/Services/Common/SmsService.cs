using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

using Microsoft.FeatureManagement;

using Newtonsoft.Json;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
{
    public interface ISmsService
    {
        Task DoSendSmsAsync(string countryCode, string mobile, string message, bool onboard = false);

        Task<string> SendSmsAsync(string countryCode, string mobile = "+61432854196", string message = "Testing fom Azy");
    }

    internal class SmsService : ISmsService
    {
        private readonly IFeatureManager featureManager;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<SmsService> logger;

        public SmsService(IFeatureManager featureManager, IHttpClientFactory httpClientFactory, ILogger<SmsService> logger)
        {
            this.featureManager = featureManager;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        public async Task DoSendSmsAsync(string countryCode, string mobile, string message, bool onboard = false)
        {
            if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN) || onboard)
            {
                await SendSmsAsync(countryCode, mobile, message);
            }
        }

        public async Task<string> SendSmsAsync(string countryCode, string mobile = "+61432854196", string message = "Testing fom Azy")
        {
            try
            {
                mobile = mobile.StartsWith("+") ? mobile : "+" + mobile;
                //var localIps = GetActiveIPAddressesInNetwork();
                var url = EnvHelper.Get("SMS_Url");

                var username = countryCode.ToLower(CultureInfo.InvariantCulture) == "au" ? EnvHelper.Get("SMS_User") : EnvHelper.Get("SMS_User_India");
                var password = countryCode.ToLower(CultureInfo.InvariantCulture) == "au" ? EnvHelper.Get("SMS_PWD") : EnvHelper.Get("SMS_PWD_INDIA");
                var sim = countryCode.ToLower(CultureInfo.InvariantCulture) == "au" ? EnvHelper.Get("SMS_Sim") : EnvHelper.Get("SMS_Sim_India");
                var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                var httpClient = httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

                var newContent = new { message = message, phoneNumbers = new List<string> { mobile }, simNumber = int.Parse(sim) };
                var jsonContent = JsonConvert.SerializeObject(newContent);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);

                // Log the response
                Console.WriteLine($"Response Status Code: {response.StatusCode}");
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response: " + responseBody);
                response.EnsureSuccessStatusCode();
                return responseBody;
            }
            catch (Exception ex)
            {
                logger.LogError("Error sending SMS to Mobile {Number} with Error {Message}: ", mobile, ex.Message);
                return string.Empty;
            }
        }
    }
}