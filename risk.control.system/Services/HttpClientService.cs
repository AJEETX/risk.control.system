using Newtonsoft.Json;

using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IHttpClientService
    {
        Task<List<PincodeApiData>> GetPinCodeLatLng(string pinCode);
    }

    public class HttpClientService : IHttpClientService
    {
        private HttpClient client = new HttpClient();

        public async Task<List<PincodeApiData>> GetPinCodeLatLng(string pinCode)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://india-pincode-with-latitude-and-longitude.p.rapidapi.com/api/v1/pincode/{pinCode}"),
                Headers =
                            {
                                { "X-RapidAPI-Key", "327fd8beb9msh8a441504790e80fp142ea8jsnf74b9208776a" },
                                { "X-RapidAPI-Host", "india-pincode-with-latitude-and-longitude.p.rapidapi.com" },
                            },
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var pinCodeData = JsonConvert.DeserializeObject<List<PincodeApiData>>(body);
                return pinCodeData;
            }
        }
    }
}