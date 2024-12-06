using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Azure;
using Highsoft.Web.Mvc.Charts;
using Newtonsoft.Json;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Api;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using Amazon.S3.Transfer;
using Amazon.Textract;

namespace risk.control.system.Services
{
    public interface IHttpClientService
    {
        Task<List<PincodeApiData>> GetPinCodeLatLng(string pinCode);

        Task<FaceImageDetail> GetMaskedImage(MaskImage image, string baseUrl);

        Task<FaceMatchDetail> GetFaceMatch(MatchImage image, string baseUrl);

        Task<PanResponse?> VerifyPanNew(string pan, string panUrl, string key, string host);

        Task<RootObject> GetAddress(string lat, string lon);
        Task<string> GetRawAddress(string lat, string lon);

        Task<LocationDetails_IpApi> GetAddressFromIp(string ipAddress);
        Task<bool> WhitelistIP(string url, string domain, string ipaddress);

        Task<bool> VerifyPassport(string passport, string dateOfBirth);

        Task<PassportOcrData> GetPassportOcrResult(byte[] imageBytes, string url, string key, string host);

        Task<AudioTranscript> TranscribeAsync(string bucketName, string fileName, string filePath);
    }

    public class HttpClientService : IHttpClientService
    {
        private HttpClient httpClient = new HttpClient();
        private static string RapidAPIHost = "idfy-verification-suite.p.rapidapi.com";
        private static string PinCodeBaseUrl = "https://india-pincode-with-latitude-and-longitude.p.rapidapi.com/api/v1/pincode";
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IAmazonTranscribeService _amazonTranscribeService;
        private readonly IAmazonS3 s3Client;
        public HttpClientService(IWebHostEnvironment webHostEnvironment, IAmazonTranscribeService amazonTranscribeService, IAmazonS3 s3Client)
        {
            this.webHostEnvironment = webHostEnvironment;
            _amazonTranscribeService = amazonTranscribeService;
            this.s3Client = s3Client;
        }
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
            var error = await response.Content.ReadAsStringAsync();

            return null;
        }

        public async Task<FaceMatchDetail> GetFaceMatch(MatchImage image, string baseUrl)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(baseUrl + "/faceMatch", image);

                if (response.IsSuccessStatusCode)
                {
                    var maskedImage = await response.Content.ReadAsStringAsync();

                    var facematchDetail = JsonConvert.DeserializeObject<FaceMatchDetail>(maskedImage);

                    return facematchDetail;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return null!;
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

        public async Task<RootObject> GetAddress(string lat, string lon)
        {
            httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            httpClient.DefaultRequestHeaders.Add("Referer", "http://www.microsoft.com");
            var result = await httpClient.GetAsync("http://nominatim.openstreetmap.org/reverse?format=json&lat=" + lat + "&lon=" + lon);
            //var rootObject = await httpClient.GetFromJsonAsync<RootObject>("http://nominatim.openstreetmap.org/reverse?format=json&lat=" + lat + "&lon=" + lon);
            var responseBody = await result.Content.ReadAsStringAsync();
            try
            {
                var rootObject = JsonConvert.DeserializeObject<RootObject>(responseBody);
                //DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(RootObject));
                //RootObject rootObject = (RootObject)ser.ReadObject(new MemoryStream(jsonData));
                return rootObject;
            }
            catch (Exception)
            {
                return new RootObject
                {
                    display_name = "Troy Court, Forest Hill, Melbourne, City of Whitehorse, Victoria, 3131, Australia"
                };
            }

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
                HttpResponseMessage httpResponse = await httpClient.PostAsJsonAsync(relativeUrl, new IPWhitelistRequest { Domain = domain, IpAddress = ipaddress, Url = url });

                if (httpResponse.IsSuccessStatusCode)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
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
                return null!;
            }
        }

        public async Task<AudioTranscript> TranscribeAsync(string bucketName, string fileName, string filePath)
        {
            try
            {
                await UploadS3Async(bucketName, fileName, filePath);

                var mediaFileUri = $"https://s3.{RegionEndpoint.APSoutheast2.SystemName}.amazonaws.com/{bucketName}/{fileName}";
                var jobRequest = new StartTranscriptionJobRequest
                {
                    TranscriptionJobName = $"audio2text-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    LanguageCode = "en-US",
                    MediaFormat = "mp3",
                    Media = new Media
                    {
                        MediaFileUri = mediaFileUri
                    },
                    OutputBucketName = bucketName
                };


                // Start the transcription job
                var jobResponse = await _amazonTranscribeService.StartTranscriptionJobAsync(jobRequest);
                Console.WriteLine($"Transcription job started. Job Status: {jobResponse.TranscriptionJob.TranscriptionJobStatus}");

                // Check if the job started successfully
                if (jobResponse.HttpStatusCode == HttpStatusCode.OK && jobResponse.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.IN_PROGRESS)
                {
                    Console.WriteLine("Transcription job is in progress...");
                }
                else
                {
                    Console.WriteLine("Failed to start transcription job.");
                    return null;
                }

                TranscriptionJobStatus jobStatus;
                GetTranscriptionJobResponse jobResponseCompleted;
                do
                {
                    // Fetch the current status of the job using GetTranscriptionJobAsync
                    jobResponseCompleted = await _amazonTranscribeService.GetTranscriptionJobAsync(new GetTranscriptionJobRequest
                    {
                        TranscriptionJobName = jobRequest.TranscriptionJobName
                    });

                    jobStatus = jobResponseCompleted.TranscriptionJob.TranscriptionJobStatus;

                    // Wait for 5 seconds before checking the status again
                    if (jobStatus == TranscriptionJobStatus.IN_PROGRESS)
                    {
                        Console.WriteLine("Transcription job is still in progress...");
                        await Task.Delay(500); // Wait 5 seconds before checking again
                    }

                } while (jobStatus == TranscriptionJobStatus.IN_PROGRESS);

                if (jobStatus == TranscriptionJobStatus.COMPLETED)
                {
                    Console.WriteLine("Transcription job completed successfully");
                    var getObjectRequest = new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = jobRequest.TranscriptionJobName + ".json"
                    };

                    using (var response = await s3Client.GetObjectAsync(getObjectRequest))
                    using (var responseStream = response.ResponseStream)
                    using (var reader = new StreamReader(responseStream))
                    {
                        string transcriptionText = await reader.ReadToEndAsync();
                        var transcriptionResult = JsonConvert.DeserializeObject<AudioTranscript>(transcriptionText);
                        Console.WriteLine("Transcription Text: ");
                        Console.WriteLine(transcriptionText);
                        return transcriptionResult;
                        // Process the transcription text as needed
                    }
                }
                else
                {
                    Console.WriteLine("Transcription job failed or was cancelled.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return null;
        }
        public async Task UploadS3Async(string bucketName, string fileName, string filePath)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = fileName,
                FilePath = filePath
            };
            try
            {
                //await CreateBucketAsync(putRequest.BucketName);


                var transferUtility = new TransferUtility(s3Client);

                await transferUtility.UploadAsync(filePath, bucketName);
                var response = await s3Client.PutObjectAsync(putRequest);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("File uploaded successfully");
                }
                //var bucketResponse = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
                //{
                //    BucketName = bucketName
                //});
                //foreach (var obj in bucketResponse.S3Objects)
                //{
                //    Console.WriteLine($"Object Key: {obj.Key}");
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
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
            //if (await DoesBucketExistAsync(bucketName))
            //{
            //    Console.WriteLine($"Bucket '{bucketName}' already exists.");
            //    return;
            //}

            // Create the bucket
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