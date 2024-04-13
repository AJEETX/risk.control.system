using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;

using Azure;

using Newtonsoft.Json;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Api;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IHttpClientService
    {
        Task<List<PincodeApiData>> GetPinCodeLatLng(string pinCode);

        Task<FaceImageDetail> GetMaskedImage(MaskImage image, string baseUrl);

        Task<FaceMatchDetail> GetFaceMatch(MatchImage image, string baseUrl);

        Task<PanVerifyResponse?> VerifyPan(string pan, string panUrl, string rapidAPIKey, string task_id, string group_id);

        Task<RootObject> GetAddress(string lat, string lon);

        Task<LocationDetails_IpApi> GetAddressFromIp(string ipAddress);
        Task<bool> WhitelistIP(string url, string domain, string ipaddress);
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
                HttpHeaders headers = response2.Headers;
                IEnumerable<string> values;
                if (headers.TryGetValues("x-ratelimit-requests-remaining", out values))
                {
                    verifiedPanResponse.count_remain = values.First();
                }
                return verifiedPanResponse;
            }
            return null!;
        }

        public async Task<RootObject> GetAddress(string lat, string lon)
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            webClient.Headers.Add("Referer", "http://www.microsoft.com");
            var jsonData = webClient.DownloadData("http://nominatim.openstreetmap.org/reverse?format=json&lat=" + lat + "&lon=" + lon);
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(RootObject));
            RootObject rootObject = (RootObject)ser.ReadObject(new MemoryStream(jsonData));
            return rootObject;
        }

        public async Task<LocationDetails_IpApi> GetAddressFromIp(string ipAddress)
        {
            var Ip_Api_Url = $"{Applicationsettings.IP_SITE}{ipAddress}"; // 206.189.139.232 - This is a sample IP address. You can pass yours if you want to test

            // Use HttpClient to get the details from the Json response
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                // Pass API address to get the Geolocation details
                httpClient.BaseAddress = new Uri(Ip_Api_Url);
                HttpResponseMessage httpResponse = await httpClient.GetAsync(Ip_Api_Url);
                // If API is success and receive the response, then get the location details
                if (httpResponse.IsSuccessStatusCode)
                {
                    var geolocationInfo = await httpResponse.Content.ReadFromJsonAsync<LocationDetails_IpApi>();
                    if (geolocationInfo != null)
                    {
                        Console.WriteLine("Country: " + geolocationInfo.country);
                        Console.WriteLine("Region: " + geolocationInfo.regionName);
                        Console.WriteLine("City: " + geolocationInfo.city);
                        Console.WriteLine("Zip: " + geolocationInfo.zip);
                        //Console.ReadKey();
                        return geolocationInfo;
                    }
                }
            }
            return null!;
        }

        public async Task<bool> WhitelistIP(string url, string domain, string ipaddress)
        {
            string relativeUrl = "api/agent/setip";
            try
            {
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.BaseAddress = new Uri(url);
                HttpResponseMessage httpResponse = await httpClient.PostAsJsonAsync(relativeUrl,new IPWhitelistRequest { Domain = domain, IpAddress = ipaddress, Url = url });

                if (httpResponse.IsSuccessStatusCode)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {

                throw;
            }
            
        }
    }
}