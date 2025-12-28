using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace risk.control.system.Services
{
    public interface IAmazonService
    {
        Task<(bool, float, Amazon.Rekognition.Model.BoundingBox?)> FaceMatch(byte[] originalImage, byte[] targetImage);
        Task<CompareFacesResponse> CompareFaceMatch(byte[] originalImage, byte[] targetImage);
        Task<DetectDocumentTextResponse> ExtractTextAsync(byte[] bytes);
    }
    internal class AmazonService : IAmazonService
    {
        private readonly IAmazonRekognition rekognitionClient;
        private readonly IAmazonTextract textractClient;
        private readonly ILogger<AmazonService> logger;

        public AmazonService(IAmazonRekognition rekognitionClient, IAmazonTextract textractClient, ILogger<AmazonService> logger)
        {
            this.rekognitionClient = rekognitionClient;
            this.textractClient = textractClient;
            this.logger = logger;
        }
        public async Task<(bool, float, Amazon.Rekognition.Model.BoundingBox?)> FaceMatch(byte[] originalImage, byte[] targetImage)
        {
            float similarityThreshold = 70F;
            Image imageSource = new();
            Image imageTarget = new();

            try
            {
                imageSource.Bytes = new MemoryStream(originalImage);
                imageTarget.Bytes = new MemoryStream(targetImage);

                var compareFacesRequest = new CompareFacesRequest
                {
                    SourceImage = imageSource,
                    TargetImage = imageTarget,
                    SimilarityThreshold = similarityThreshold,
                };

                var compareFacesResponse = await rekognitionClient.CompareFacesAsync(compareFacesRequest);
                var result = compareFacesResponse.FaceMatches.Count >= 1
                    && compareFacesResponse.FaceMatches[0].Similarity >= similarityThreshold;
                var faceBox = result ? compareFacesResponse.FaceMatches[0].Face.BoundingBox : compareFacesResponse.UnmatchedFaces[0].BoundingBox;
                var similarity = result ? compareFacesResponse.FaceMatches[0].Similarity.GetValueOrDefault() : 0;
                return (result, similarity, faceBox);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to compare faces");
                return (false, 0, null);
            }
        }

        public async Task<DetectDocumentTextResponse> ExtractTextAsync(byte[] bytes)
        {
            try
            {
                var detectResponse = await textractClient.DetectDocumentTextAsync(new DetectDocumentTextRequest
                {
                    Document = new Document
                    {
                        Bytes = new MemoryStream(bytes)
                    }
                });

                foreach (var block in detectResponse.Blocks)
                {
                    logger.LogInformation($"Type {block.BlockType}, Text: {block.Text}");
                }
                return detectResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to extract texts");
                return null!;
            }

        }

        public async Task<CompareFacesResponse> CompareFaceMatch(byte[] originalImage, byte[] targetImage)
        {
            float similarityThreshold = 70F;
            Image imageSource = new();
            Image imageTarget = new();

            try
            {
                imageSource.Bytes = new MemoryStream(originalImage);
                imageTarget.Bytes = new MemoryStream(targetImage);

                var compareFacesRequest = new CompareFacesRequest
                {
                    SourceImage = imageSource,
                    TargetImage = imageTarget,
                    SimilarityThreshold = similarityThreshold,
                };

                var compareFacesResponse = await rekognitionClient.CompareFacesAsync(compareFacesRequest);
                return compareFacesResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to compare faces");
                return null!;
            }
        }
    }
}
