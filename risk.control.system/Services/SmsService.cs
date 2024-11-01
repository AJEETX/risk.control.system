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

        public static async Task Send()
        {
            var messages = new SMSBody { messages = new List<Message> { new Message { channel = "sms", originator = "icheckify", recipients = new List<string> { "+61432854196" }, content = "greet from icheckify", data_coding = "text" } } };

            var content = JsonConvert.SerializeObject(messages, Formatting.Indented);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://d7sms.p.rapidapi.com/messages/v1/send"),
                Headers =
                    {
                        { "Token", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJhdXRoLWJhY2tlbmQ6YXBwIiwic3ViIjoiZDQ0OWVmODAtOTEwNS00ZWNiLTgwYzctNDNkMDQ1YmEwMjhjIn0.6wR1Jw3j3j-_oT6foTBkqT-eGEEsc1P6ntBV08dS3zw" },
                        { "X-RapidAPI-Key", "47cd2be148msh455c39da6e1d554p1733e0jsn8bd7464ed610" },
                        { "X-RapidAPI-Host", "d7sms.p.rapidapi.com" },
                    },
                Content = new StringContent(content)
                //Content = new StringContent("{\r\"messages\": [\r{\r\"channel\": \"sms\",\r \"originator\": \"D7-RapidAPI\",\r\"recipients\": [\r\"+9715097526xx\",\r\"+9715097526xx\"\r],\r\"content\": \"Greetings from D7 API \",\r\"data_coding\": \"text\"\r }\r]\r}")
                {
                    Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/json")
                        }
                }
            };

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine(body);
            }
        }

        public static Dictionary<string, object> SendSingleMessage(string mobile = "61432854196", string message = "Testing fom Azy", bool active = false, string? device = "0", long? timestamp = null, bool isMMS = false, string? attachments = null, bool priority = false)
        {
            // Send a message using the primary device.
            if (active)
            {
                SendSmsAsync("+" + mobile, message).RunSynchronously();
                return null;
            }
            return null;
        }
        public static async Task SendSmsAsync(string mobile = "+61432854196", string message = "Testing fom Azy")
        {
            var url = "https://api.sms-gate.app/3rdparty/v1/message";
            var username = "YXNGBE"; 
            var password = "rfi-gbbukll7-6"; 
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            var newContent = new { message = message, phoneNumbers = new List<string> { mobile } }; 
            var jsonContent = JsonConvert.SerializeObject(newContent); 
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            try 
            { 
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