using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace risk.control.system.Services.Agent
{
    public interface IAmazonApiService
    {
        Task<CollectionStatus> EnsureCollectionExistsAsync(string collectionId);

        Task<DeleteCollectionResponse> DeleteCollectionAsync(string collectionId);

        Task<IndexFacesResponse> IndexFacesAsync(IndexFacesRequest request);

        Task<SearchFacesByImageResponse> SearchFacesByImageAsync(SearchFacesByImageRequest request);

        Task<DeleteFacesResponse> DeleteFacesAsync(string collectionId, List<string> faceIds);

        Task<(bool, float, Amazon.Rekognition.Model.BoundingBox?)> FaceMatch(byte[] originalImage, byte[] targetImage);

        Task<CompareFacesResponse> CompareFaceMatch(byte[] originalImage, byte[] targetImage);

        Task<DetectDocumentTextResponse> ExtractTextAsync(byte[] bytes);
    }

    public enum CollectionStatus { Existing, Created, Failed }

    internal class AmazonApiService(IAmazonRekognition rekognitionClient, IAmazonTextract textractClient, ILogger<AmazonApiService> logger) : IAmazonApiService
    {
        private readonly IAmazonRekognition _rekognitionClient = rekognitionClient;
        private readonly IAmazonTextract _textractClient = textractClient;
        private readonly ILogger<AmazonApiService> _logger = logger;

        public async Task<CollectionStatus> EnsureCollectionExistsAsync(string collectionId)
        {
            var sanitizedId = collectionId?.Replace("\n", "").Replace("\r", "");
            try
            {
                var response = await _rekognitionClient.DescribeCollectionAsync(new DescribeCollectionRequest
                {
                    CollectionId = sanitizedId
                });
                if (response.UserCount > 0)
                {
                    return CollectionStatus.Existing;
                }
            }
            catch (Amazon.Rekognition.Model.ResourceNotFoundException ex)
            {
                _logger.LogInformation($"Collection {sanitizedId} not found. Creating new collection.{ex.Message}");
                var createdResponse = await _rekognitionClient.CreateCollectionAsync(new CreateCollectionRequest
                {
                    CollectionId = sanitizedId
                });
                return createdResponse.StatusCode == (int)System.Net.HttpStatusCode.OK ? CollectionStatus.Created : CollectionStatus.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error ensuring collection: {ex.Message}");
            }
            return CollectionStatus.Failed;
        }

        public async Task<DeleteCollectionResponse> DeleteCollectionAsync(string collectionId)
        {
            try
            {
                var sanitizedId = collectionId?.Replace("\n", "").Replace("\r", "");

                var response = await _rekognitionClient.DescribeCollectionAsync(new DescribeCollectionRequest
                {
                    CollectionId = sanitizedId
                });
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK && response.UserCount > 0)
                {
                    var request = new DeleteCollectionRequest
                    {
                        CollectionId = sanitizedId
                    };
                    return await _rekognitionClient.DeleteCollectionAsync(request);
                }
                else
                {
                    _logger.LogInformation($"Collection {sanitizedId} does not exist. No need to delete.");
                    return new DeleteCollectionResponse { StatusCode = (int)System.Net.HttpStatusCode.NotFound };
                }
            }
            catch (Amazon.Rekognition.Model.ResourceNotFoundException ex)
            {
                _logger.LogError($"Error ensuring collection: {ex.Message}");
                return new DeleteCollectionResponse { StatusCode = (int)ex.StatusCode };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error ensuring collection: {ex.Message}");
                return new DeleteCollectionResponse { StatusCode = (int)System.Net.HttpStatusCode.InternalServerError };
            }
        }

        public async Task<IndexFacesResponse> IndexFacesAsync(IndexFacesRequest request)
        {
            return await _rekognitionClient.IndexFacesAsync(request);
        }

        public async Task<SearchFacesByImageResponse> SearchFacesByImageAsync(SearchFacesByImageRequest request)
        {
            return await _rekognitionClient.SearchFacesByImageAsync(request);
        }

        public async Task<DeleteFacesResponse> DeleteFacesAsync(string collectionId, List<string> faceIds)
        {
            var sanitizedId = collectionId?.Replace("\n", "").Replace("\r", "");
            var request = new DeleteFacesRequest
            {
                CollectionId = sanitizedId,
                FaceIds = faceIds
            };
            return await _rekognitionClient.DeleteFacesAsync(request);
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

                var compareFacesResponse = await _rekognitionClient.CompareFacesAsync(compareFacesRequest);
                var result = compareFacesResponse.FaceMatches.Count >= 1
                    && compareFacesResponse.FaceMatches[0].Similarity >= similarityThreshold;
                var faceBox = result ? compareFacesResponse.FaceMatches[0].Face.BoundingBox : compareFacesResponse.UnmatchedFaces[0].BoundingBox;
                var similarity = result ? compareFacesResponse.FaceMatches[0].Similarity.GetValueOrDefault() : 0;
                return (result, similarity, faceBox);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compare faces");
                return (false, 0, null);
            }
        }

        public async Task<DetectDocumentTextResponse> ExtractTextAsync(byte[] bytes)
        {
            try
            {
                var detectResponse = await _textractClient.DetectDocumentTextAsync(new DetectDocumentTextRequest
                {
                    Document = new Document
                    {
                        Bytes = new MemoryStream(bytes)
                    }
                });

                foreach (var block in detectResponse.Blocks)
                {
                    _logger.LogInformation($"Type {block.BlockType}, Text: {block.Text}");
                }
                return detectResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract texts");
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

                var compareFacesResponse = await _rekognitionClient.CompareFacesAsync(compareFacesRequest);
                return compareFacesResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compare faces");
                return null!;
            }
        }
    }
}