using System.Net.Http.Headers;

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
    }
}