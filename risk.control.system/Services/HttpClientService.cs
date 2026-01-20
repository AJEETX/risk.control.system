using System.Net.Http.Headers;

using Amazon.S3;
using Amazon.TranscribeService;

using Newtonsoft.Json;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IHttpClientService
    {

        Task<PanResponse?> VerifyPanNew(string pan, string panUrl, string key, string host);

        Task<string> GetRawAddress(string lat, string lon);

        Task<bool> VerifyPassport(string passport, string dateOfBirth);

        Task<PassportOcrData> GetPassportOcrResult(byte[] imageBytes, string url, string key, string host);

    }

    internal class HttpClientService : IHttpClientService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<HttpClientService> logger;
        private readonly IWebHostEnvironment env;
        private readonly IAmazonTranscribeService _amazonTranscribeService;
        private readonly IAmazonS3 s3Client;
        private readonly IMediaDataService mediaDataService;

        public HttpClientService(
            IHttpClientFactory httpClientFactory,
            ILogger<HttpClientService> logger,
            IWebHostEnvironment env,
            IAmazonTranscribeService amazonTranscribeService,
            IAmazonS3 s3Client,
            IMediaDataService mediaDataService)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.env = env;
            _amazonTranscribeService = amazonTranscribeService;
            this.s3Client = s3Client;
            this.mediaDataService = mediaDataService;
        }

        public async Task<string> GetRawAddress(string lat, string lon)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://api.geoapify.com/v1/geocode/reverse?lat={lat}&lon={lon}&apiKey={Applicationsettings.REVERRSE_GEOCODING}"),
            };
            try
            {
                var httpClient = httpClientFactory.CreateClient();
                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var addressData = JsonConvert.DeserializeObject<MapAddress>(body);
                    return (addressData.features.FirstOrDefault().properties.formatted);
                }
            }
            catch (Exception)
            {
                return "Troy Court, Forest Hill, Melbourne, City of Whitehorse, Victoria, 3131, Australia";
            }

        }

        public async Task<PanResponse?> VerifyPanNew(string pan, string panUrl, string key, string host)
        {
            var payload = new PanRequest { PAN = pan };
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(panUrl),
                Headers =
                {
                    { "x-rapidapi-key", key },
                    { "x-rapidapi-host", host },
                },
                Content = new StringContent(JsonConvert.SerializeObject(payload))
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };
            var httpClient = httpClientFactory.CreateClient();

            using (var response = await httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine(body);
                var panResponse = JsonConvert.DeserializeObject<PanResponse>(body);
                return panResponse;
            }
        }

        public async Task<bool> VerifyPassport(string passport, string dateOfBirth)
        {

            var requestId = await StartVerifyPassport(passport, dateOfBirth);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://passport-verification.p.rapidapi.com/v3/tasks?request_id={requestId}"),
                Headers =
                {
                    { "x-rapidapi-key", "327fd8beb9msh8a441504790e80fp142ea8jsnf74b9208776a" },
                    { "x-rapidapi-host", "passport-verification.p.rapidapi.com" },
                },
            };
            var httpClient = httpClientFactory.CreateClient();
            using (var response = await httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine(body);
            }
            return true;
        }

        public async Task<PassportOcrData> GetPassportOcrResult(byte[] imageBytes, string url, string key, string host)
        {
            var extension = risk.control.system.Helpers.Extensions.GetImageExtension(imageBytes);
            // Convert the byte array to a Base64 string

            string filePath = $"{Guid.NewGuid()}.{extension}";
            string path = Path.Combine(env.WebRootPath, "passport");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var imagefilePath = Path.Combine(env.WebRootPath, "passport", filePath);
            // Write the byte array to a file
            File.WriteAllBytes(imagefilePath, imageBytes);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Headers =
                {
                    { "x-rapidapi-key", key },
                    { "x-rapidapi-host", host },
                },
                Content = new MultipartFormDataContent
                {
                    new StringContent(imagefilePath)
                    {
                        Headers =
                        {
                            ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = "inputimage",
                            }
                        }
                    },
                },
            };
            try
            {
                var httpClient = httpClientFactory.CreateClient();
                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var passportOcrData = JsonConvert.DeserializeObject<PassportOcrData>(body);
                    Console.WriteLine(body);
                    return passportOcrData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null!;
            }
        }

        private async Task<string> StartVerifyPassport(string passport, string date_of_birth)
        {
            var content = new
            {
                task_id = "74f4c926-250c-43ca-9c53-453e87ceacd1",
                group_id = "8e16424a-58fc-4ba4-ab20-5bc8e7c3c41e",
                data = new
                {
                    passport_file_number = passport,
                    date_of_birth = date_of_birth
                }
            };
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://passport-verification.p.rapidapi.com/v3/tasks/async/verify_with_source/ind_passport"),
                Headers =
                {
                    { "x-rapidapi-key", "327fd8beb9msh8a441504790e80fp142ea8jsnf74b9208776a" },
                    { "x-rapidapi-host", "passport-verification.p.rapidapi.com" },
                },

                Content = new StringContent(JsonConvert.SerializeObject(content))
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };
            var httpClient = httpClientFactory.CreateClient();
            using (var response = await httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine(body);
                var requestId = JsonConvert.DeserializeObject<List<PassportResult>>(body).FirstOrDefault()?.request_id;
                return requestId;
            }
        }
    }
}