using System.Net.Http.Headers;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;

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

        Task<AudioTranscript> TranscribeAsync(long locationId, string reportName, string bucketName, string fileName, string filePath);
    }

    public class HttpClientService : IHttpClientService
    {
        private static HttpClient httpClient = new HttpClient();
        private static string PinCodeBaseUrl = "https://india-pincode-with-latitude-and-longitude.p.rapidapi.com/api/v1/pincode";
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IAmazonTranscribeService _amazonTranscribeService;
        private readonly IAmazonS3 s3Client;
        private readonly IMediaDataService mediaDataService;

        public HttpClientService(IWebHostEnvironment webHostEnvironment, IAmazonTranscribeService amazonTranscribeService, IAmazonS3 s3Client, IMediaDataService mediaDataService)
        {
            this.webHostEnvironment = webHostEnvironment;
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
            string path = Path.Combine(webHostEnvironment.WebRootPath, "passport");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var imagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "passport", filePath);
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

        public async Task<AudioTranscript> TranscribeAsync(long locationId, string reportName, string bucketName, string fileName, string filePath)
        {
            try
            {
                await UploadS3Async(bucketName, fileName, filePath);

                var mediaFileUri = $"https://{bucketName}.s3.{RegionEndpoint.APSoutheast2.SystemName}.amazonaws.com/{fileName}";

                var jobRequest = new StartTranscriptionJobRequest
                {
                    TranscriptionJobName = $"audio2text-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    LanguageCode = "en-US",
                    MediaFormat = "mp3",
                    Media = new Media { MediaFileUri = mediaFileUri },
                    OutputBucketName = bucketName
                };

                var jobResponse = await _amazonTranscribeService.StartTranscriptionJobAsync(jobRequest);
                Console.WriteLine($"Transcription job started: {jobResponse.HttpStatusCode}");

                // Polling until completion
                GetTranscriptionJobResponse jobResponseCompleted;
                do
                {
                    await Task.Delay(5000);
                    jobResponseCompleted = await _amazonTranscribeService.GetTranscriptionJobAsync(
                        new GetTranscriptionJobRequest { TranscriptionJobName = jobRequest.TranscriptionJobName });

                    Console.WriteLine($"Job status: {jobResponseCompleted.TranscriptionJob.TranscriptionJobStatus}");

                } while (jobResponseCompleted.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.IN_PROGRESS);

                if (jobResponseCompleted.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED)
                {
                    Console.WriteLine("Transcription completed.");
                    var getObjectRequest = new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = $"{jobRequest.TranscriptionJobName}.json"
                    };

                    using var response = await s3Client.GetObjectAsync(getObjectRequest);
                    using var reader = new StreamReader(response.ResponseStream);
                    string transcriptionText = await reader.ReadToEndAsync();

                    await mediaDataService.SaveTranscript(locationId, reportName, transcriptionText);
                    return JsonConvert.DeserializeObject<AudioTranscript>(transcriptionText);
                }

                Console.WriteLine("Transcription failed or cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return null;
        }

        public async Task UploadS3Async(string bucketName, string fileName, string filePath)
        {
            try
            {
                await CreateBucketAsync(bucketName);
                var regionResponse = await s3Client.GetBucketLocationAsync(bucketName);

                var transferUtility = new TransferUtility(s3Client);
                await transferUtility.UploadAsync(filePath, bucketName, fileName);

                Console.WriteLine("File uploaded successfully");
            }
            catch (AmazonS3Exception s3Ex)
            {
                Console.WriteLine($"S3 Error: {s3Ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
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
            using (var response = await httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine(body);
                var requestId = JsonConvert.DeserializeObject<List<PassportResult>>(body).FirstOrDefault()?.request_id;
                return requestId;
            }
        }

        private async Task CreateBucketAsync(string bucketName)
        {
            // Check if bucket already exists
            if (await DoesBucketExistAsync(bucketName))
            {
                Console.WriteLine($"Bucket '{bucketName}' already exists.");
                return;
            }
            try
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true // Automatically uses the region of the client
                };
                var response = await s3Client.PutBucketAsync(putBucketRequest);
                Console.WriteLine($"Bucket created with HTTP status code: {response.HttpStatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async Task<bool> DoesBucketExistAsync(string bucketName)
        {
            try
            {
                return await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown: {ex.Message}");
                return false;
            }
            //return await s3Client.DoesS3BucketExistAsync(bucketName);
        }
    }
}