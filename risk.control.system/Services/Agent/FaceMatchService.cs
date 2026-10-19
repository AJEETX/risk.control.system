using Amazon.Rekognition.Model;
using risk.control.system.Models;
using SixLabors.ImageSharp;

namespace risk.control.system.Services.Agent
{
    public interface IFaceMatchService
    {
        Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes);

        Task<FaceAnalysisResult> GetPersonDetailFromFace(byte[] faceImage);
    }

    internal class FaceMatchService(IAmazonApiService amazonApiService, IProcessImageService processImageService, ILogger<FaceMatchService> logger) : IFaceMatchService
    {
        private readonly IAmazonApiService _amazonApiService = amazonApiService;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly ILogger<FaceMatchService> _logger = logger;

        public async Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes)
        {
            try
            {
                var matched = await _amazonApiService.FaceMatch(registeredImage, faceImageBytes);
                return (matched.Item1) ?
                    (matched.Item2.ToString(), _processImageService.CompressAndWaterMarkFace(faceImageBytes, 80, "MATCH", Color.White), matched.Item2) :
                    ("0", _processImageService.CompressAndWaterMarkFace(faceImageBytes, 80, "NO-MATCH", Color.Red), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed face match");
                return ("0", _processImageService.CompressAndWaterMarkFace(faceImageBytes, 80, "NO-MATCH", Color.Red), 0);
            }
        }

        public async Task<FaceAnalysisResult> GetPersonDetailFromFace(byte[] faceImage)
        {
            try
            {
                await using var ms = new MemoryStream(faceImage);

                var request = new DetectFacesRequest
                {
                    Image = new Amazon.Rekognition.Model.Image { Bytes = ms },
                    Attributes = new List<string> { "ALL" }
                };

                var response = await _amazonApiService.ValidateSingleFace(request);
                var faceDetail = MapFaceDetails(response);
                return faceDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed face match");
                return null!;
            }
        }
        private static FaceAnalysisResult MapFaceDetails(DetectFacesResponse response)
        {
            var result = new FaceAnalysisResult();

            if (response?.FaceDetails == null || response.FaceDetails.Count == 0)
            {
                result.TotalFacesDetected = 0;
                return result;
            }

            result.TotalFacesDetected = response.FaceDetails.Count;

            for (int i = 0; i < response.FaceDetails.Count; i++)
            {
                var face = response.FaceDetails[i];

                // Find the emotion with the highest confidence score
                var primaryEmotion = face.Emotions?.OrderByDescending(e => e.Confidence).FirstOrDefault();

                FaceDetailModel? faceModel;
                if (face.BoundingBox != null)
                {
                    faceModel = new FaceDetailModel
                    {
                        FaceNumber = i + 1,
                        AgeRange = face.AgeRange != null ? $"{face.AgeRange.Low}-{face.AgeRange.High}" : "Unknown",
                        Gender = face.Gender?.Value?.Value ?? "Unknown",
                        GenderConfidence = face.Gender?.Confidence ?? 0f,
                        PrimaryEmotion = primaryEmotion?.Type?.Value ?? "Unknown",
                        EmotionConfidence = primaryEmotion?.Confidence ?? 0f,
                        IsSmiling = face.Smile?.Value ?? false,
                        HasBeard = face.Beard?.Value ?? false,
                        IsWearingGlasses = face.Eyeglasses?.Value ?? false
                    };
                }
                else
                {
                    faceModel = new FaceDetailModel
                    {
                        FaceNumber = i + 1,
                        AgeRange = face.AgeRange != null ? $"{face.AgeRange.Low}-{face.AgeRange.High}" : "Unknown",
                        Gender = face.Gender?.Value?.Value ?? "Unknown",
                        GenderConfidence = face.Gender?.Confidence ?? 0f,
                        PrimaryEmotion = primaryEmotion?.Type?.Value ?? "Unknown",
                        EmotionConfidence = primaryEmotion?.Confidence ?? 0f,
                        IsSmiling = face.Smile?.Value ?? false,
                        HasBeard = face.Beard?.Value ?? false,
                        IsWearingGlasses = face.Eyeglasses?.Value ?? false,
                    };
                }

                result.Faces.Add(faceModel);
            }

            return result;
        }
    }
}