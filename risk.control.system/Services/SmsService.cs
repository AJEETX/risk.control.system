using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using Newtonsoft.Json;

using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public class SmsService
    {
        private static HttpClient client = new HttpClient();

        public static async Task SendSmsAsync(string mobile = "+61432854196", string message = "Testing fom Azy")
        {
            try
            {
                var url = "https://api.sms-gate.app/3rdparty/v1/message";
                var username = "YXNGBE";
                var password = "rfi-gbbukll7-6";
                var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
                mobile = mobile.StartsWith("+") ? mobile : "+" + mobile;

                var newContent = new { message = message, phoneNumbers = new List<string> { mobile } };
                var jsonContent = JsonConvert.SerializeObject(newContent);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response: " + responseBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending SMS: " + ex.Message);
            }
        }
    }
}