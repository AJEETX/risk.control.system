using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

using Newtonsoft.Json;

using risk.control.system.Controllers.Api;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IHttpClientService
    {
        Task<List<PincodeApiData>> GetPinCodeLatLng(string pinCode);

        Task<FaceImageDetail> GetMaskedImage(MaskImage image, string baseUrl);

        Task<FaceMatchDetail> GetFaceMatch(MatchImage image, string baseUrl);

        Task<PanVerifyResponse?> VerifyPan(string pan, string panUrl, string rapidAPIKey, string task_id, string group_id);
    }

    public class HttpClientService : IHttpClientService
    {
        private HttpClient httpClient = new HttpClient();
        private static string RapidAPIHost = "idfy-verification-suite.p.rapidapi.com";
        private static string PinCodeBaseUrl = "https://india-pincode-with-latitude-and-longitude.p.rapidapi.com/api/v1/pincode";

        public async Task<List<PincodeApiData>> GetPinCodeLatLng(string pinCode)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{PinCodeBaseUrl}/{pinCode}"),
                Headers =
                            {
                                { "X-RapidAPI-Key", "327fd8beb9msh8a441504790e80fp142ea8jsnf74b9208776a" },
                                { "X-RapidAPI-Host", "india-pincode-with-latitude-and-longitude.p.rapidapi.com" },
                            },
            };
            using (var response = await httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var pinCodeData = JsonConvert.DeserializeObject<List<PincodeApiData>>(body);
                return pinCodeData;
            }
        }

        public async Task<FaceImageDetail> GetMaskedImage(MaskImage image, string baseUrl)
        {
            var response = await httpClient.PostAsJsonAsync(baseUrl + "/ocr", image);

            if (response.IsSuccessStatusCode)
            {
                var maskedImage = await response.Content.ReadAsStringAsync();

                var maskedImageDetail = JsonConvert.DeserializeObject<FaceImageDetail>(maskedImage);

                return maskedImageDetail;
            }
            return null;
        }

        public async Task<FaceMatchDetail> GetFaceMatch(MatchImage image, string baseUrl)
        {
            var response = await httpClient.PostAsJsonAsync(baseUrl + "/faceMatch", image);

            if (response.IsSuccessStatusCode)
            {
                var maskedImage = await response.Content.ReadAsStringAsync();

                var facematchDetail = JsonConvert.DeserializeObject<FaceMatchDetail>(maskedImage);

                return facematchDetail;
            }
            return null;
        }

        public async Task<PanVerifyResponse?> VerifyPan(string pan, string panUrl, string rapidAPIKey, string task_id, string group_id)
        {
            var requestPayload = new PanVerifyRequest
            {
                task_id = task_id,
                group_id = group_id,
                data = new PanNumber
                {
                    id_number = pan
                }
            };

            var request2 = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(panUrl + "/v3/tasks/sync/verify_with_source/ind_pan"),
                Headers =
                {
                    { "X-RapidAPI-Key", rapidAPIKey },
                    { "X-RapidAPI-Host", RapidAPIHost },
                },
                Content = new StringContent(JsonConvert.SerializeObject(requestPayload)) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } },
            };

            using var response2 = await httpClient.SendAsync(request2);
            if (response2.StatusCode == HttpStatusCode.OK)
            {
                var body = await response2.Content.ReadAsStringAsync();
                var verifiedPanResponse = JsonConvert.DeserializeObject<PanVerifyResponse>(body);
                return verifiedPanResponse;
            }
            return null!;
        }
    }
}