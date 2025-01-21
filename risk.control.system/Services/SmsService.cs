using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using Microsoft.FeatureManagement;

using Newtonsoft.Json;

using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ISmsService
    {
        Task DoSendSmsAsync(string mobile, string message, bool onboard = false);
    }

    public class SmsService : ISmsService
    {
        private static HttpClient client = new HttpClient();
        private readonly IFeatureManager featureManager;

        public SmsService(IFeatureManager featureManager)
        {
            this.featureManager = featureManager;
        }

        public async Task DoSendSmsAsync(string mobile, string message, bool onboard = false)
        {
            if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN) || onboard)
            {
                await SendSmsAsync(mobile, message);
            }
        }

        public static async Task SendSmsAsync(string mobile = "+61432854196", string message = "Testing fom Azy")
        {
            try
            {
                var url = "https://api.sms-gate.app/3rdparty/v1/message";

                var username = Environment.GetEnvironmentVariable("SMS_User");
                var password = Environment.GetEnvironmentVariable("SMS_Pwd");
                var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
                mobile = mobile.StartsWith("+") ? mobile : "+" + mobile;

                var newContent = new { message = message, phoneNumbers = new List<string> { mobile } };
                var jsonContent = JsonConvert.SerializeObject(newContent);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);

                // Log the response
                Console.WriteLine($"Response Status Code: {response.StatusCode}");
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response: " + responseBody);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending SMS: " + ex.Message);
            }
        }
    }
}