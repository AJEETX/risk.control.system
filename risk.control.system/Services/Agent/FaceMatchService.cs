using System.Text;
using Amazon.Rekognition.Model;
using risk.control.system.Models;

namespace risk.control.system.Services.Agent
{
    public interface IFaceMatchService
    {
        Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes, string onlyExtension);

        Task<FaceAnalysisResult> GetPersonDetailFromFace(byte[] faceImage);
    }

    internal class FaceMatchService(IAmazonApiService amazonApiService, IProcessImageService processImageService, ILogger<FaceMatchService> logger) : IFaceMatchService
    {
        private readonly IAmazonApiService _amazonApiService = amazonApiService;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly ILogger<FaceMatchService> _logger = logger;

        public async Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes, string onlyExtension)
        {
            try
            {
                var matched = await _amazonApiService.FaceMatch(registeredImage, faceImageBytes);
                return matched.Item1 ? (matched.Item2.ToString(), _processImageService.CompressImage(faceImageBytes), matched.Item2) : ("0", _processImageService.CompressImage(faceImageBytes), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed face match");
                return ("0", _processImageService.CompressImage(faceImageBytes), 0);
            }
        }

        public async Task<FaceAnalysisResult> GetPersonDetailFromFace(byte[] faceImage)
        {
            try
            {
                await using var ms = new MemoryStream(faceImage);

                var request = new DetectFacesRequest
                {
                    Image = new Image { Bytes = ms },
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
        private static string FormatFaceDetailsToString(DetectFacesResponse response)
        {
            if (response?.FaceDetails == null || response.FaceDetails.Count == 0)
            {
                return "No faces detected in the image.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Detected {response.FaceDetails.Count} face(s):\n");

            for (int i = 0; i < response.FaceDetails.Count; i++)
            {
                var face = response.FaceDetails[i];
                sb.AppendLine($"--- Face #{i + 1} ---");

                // 1. Estimated Age Range
                if (face.AgeRange != null)
                {
                    sb.AppendLine($"Age Range: {face.AgeRange.Low} - {face.AgeRange.High} years old");
                }

                // 2. Gender (with confidence score)
                if (face.Gender != null)
                {
                    sb.AppendLine($"Gender: {face.Gender.Value} ({face.Gender.Confidence:F1}% confident)");
                }

                // 3. Emotions (Grabbing the highest confidence emotion, or listing all)
                if (face.Emotions != null && face.Emotions.Count > 0)
                {
                    // Sorting to get the primary emotion first
                    var primaryEmotion = face.Emotions.OrderByDescending(e => e.Confidence).First();
                    sb.AppendLine($"Primary Emotion: {primaryEmotion.Type} ({primaryEmotion.Confidence:F1}%)");

                    // Optional: List all detected emotions
                    var allEmotions = string.Join(", ", face.Emotions.Select(e => $"{e.Type}({e.Confidence:F0}%)"));
                    sb.AppendLine($"All Emotions: {allEmotions}");
                }

                // 4. Facial Attributes (Smile, Eyeglasses, Beard, etc.)
                sb.AppendLine($"Smiling: {face.Smile?.Value ?? false} ({face.Smile?.Confidence ?? 0:F1}%)");
                sb.AppendLine($"Wearing Glasses: {face.Eyeglasses?.Value ?? false} ({face.Eyeglasses?.Confidence ?? 0:F1}%)");
                sb.AppendLine($"Has Beard: {face.Beard?.Value ?? false} ({face.Beard?.Confidence ?? 0:F1}%)");
                sb.AppendLine($"Eyes Open: {face.EyesOpen?.Value ?? false} ({face.EyesOpen?.Confidence ?? 0:F1}%)");
                sb.AppendLine($"Mouth Open: {face.MouthOpen?.Value ?? false} ({face.MouthOpen?.Confidence ?? 0:F1}%)");

                // 5. Bounding Box (Location of the face in percentages 0-1)
                if (face.BoundingBox != null)
                {
                    sb.AppendLine($"Bounding Box: Top={face.BoundingBox.Top:F2}, Left={face.BoundingBox.Left:F2}, Width={face.BoundingBox.Width:F2}, Height={face.BoundingBox.Height:F2}");
                }

                sb.AppendLine(); // Blank line between faces
            }

            return sb.ToString();
        }
    }
}