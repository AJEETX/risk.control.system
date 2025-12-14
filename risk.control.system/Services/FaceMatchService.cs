using risk.control.system.Helpers;

namespace risk.control.system.Services
{
    public interface IFaceMatchService
    {
        Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes, string onlyExtension);
        //Task<FaceMatchResult> CompareAsync(Stream image1, Stream image2);
    }
    internal class FaceMatchService : IFaceMatchService
    {
        private readonly ICompareFaces compareFaces;
        private readonly ILogger<FaceMatchService> logger;
        //private readonly HttpClient _httpClient;
        //private readonly IConfiguration _config;


        public FaceMatchService(ICompareFaces compareFaces, ILogger<FaceMatchService> logger
            //,HttpClient httpClient, IConfiguration config
            )
        {
            this.compareFaces = compareFaces;
            this.logger = logger;
            //_httpClient = httpClient;
            //_config = config;

            //_httpClient.DefaultRequestHeaders.Add(
            //    "Ocp-Apim-Subscription-Key",
            //    _config["AzureFace:ApiKey"]);
        }
        public async Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes, string onlyExtension)
        {
            string ImageData = string.Empty;
            try
            {
                var matched = await compareFaces.DoFaceMatch(registeredImage, faceImageBytes);
                return matched.Item1 ? (matched.Item2.ToString(), CompressImage.ProcessCompress(faceImageBytes, onlyExtension, 10, 99, matched.Item3), matched.Item2) : ("0", CompressImage.ProcessCompress(faceImageBytes, onlyExtension, 10, 99, matched.Item3), 0);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed face match");
                return ("0", CompressImage.ProcessCompress(faceImageBytes, onlyExtension), 0);
            }
        }

        //public async Task<FaceMatchResult> CompareAsync(Stream image1, Stream image2)
        //{
        //    var faceId1 = await DetectFaceAsync(image1);
        //    var faceId2 = await DetectFaceAsync(image2);

        //    if (faceId1 == null || faceId2 == null)
        //    {
        //        return new FaceMatchResult
        //        {
        //            IsMatch = false,
        //            Message = "Face not detected in one or both images"
        //        };
        //    }

        //    var verifyResponse = await VerifyAsync(faceId1.Value, faceId2.Value);

        //    double threshold = double.Parse(_config["AzureFace:Threshold"]);

        //    return new FaceMatchResult
        //    {
        //        IsMatch = verifyResponse.confidence >= threshold,
        //        Confidence = verifyResponse.confidence,
        //        Message = verifyResponse.confidence >= threshold
        //            ? "Face match successful"
        //            : "Face match failed"
        //    };
        //}

        //private async Task<Guid?> DetectFaceAsync(Stream imageStream)
        //{
        //    var url = $"{_config["AzureFace:Endpoint"]}/face/v1.0/detect" +
        //              "?recognitionModel=recognition_04" +
        //              "&returnFaceId=true";

        //    using var content = new StreamContent(imageStream);
        //    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        //    var response = await _httpClient.PostAsync(url, content);
        //    if (!response.IsSuccessStatusCode) return null;

        //    var json = await response.Content.ReadAsStringAsync();
        //    var faces = JsonSerializer.Deserialize<List<DetectResponse>>(json);

        //    return faces?.FirstOrDefault()?.faceId;
        //}

        //private async Task<VerifyResponse> VerifyAsync(Guid faceId1, Guid faceId2)
        //{
        //    var url = $"{_config["AzureFace:Endpoint"]}/face/v1.0/verify";

        //    var payload = new
        //    {
        //        faceId1,
        //        faceId2
        //    };

        //    var content = new StringContent(
        //        JsonSerializer.Serialize(payload),
        //        Encoding.UTF8,
        //        "application/json");

        //    var response = await _httpClient.PostAsync(url, content);
        //    response.EnsureSuccessStatusCode();

        //    var json = await response.Content.ReadAsStringAsync();
        //    return JsonSerializer.Deserialize<VerifyResponse>(json)!;
        //}

        //private record DetectResponse(Guid faceId);
        //private record VerifyResponse(bool isIdentical, double confidence);

    }
}
