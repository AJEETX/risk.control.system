using System.Net;
using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.Textract;
using Amazon.Textract.Model;
using risk.control.system.Helpers;

namespace risk.control.system.Services.Agent
{
    public interface IAmazonApiService
    {
        Task<CollectionStatus> EnsureCollectionExistsAsync(string collectionId);
        Task<List<string>> GetAllFaceCollectionIdsAsync();
        Task<string> FaceCollectionExistAsync(string collectionId);
        Task<List<Face>> GetAllFacesFromCollectionAsync(string collectionId);
        Task<DeleteCollectionResponse> DeleteCollectionAsync(string collectionId);

        Task<IndexFacesResponse> IndexFacesAsync(IndexFacesRequest request);

        Task<SearchFacesByImageResponse> SearchFacesByImageAsync(SearchFacesByImageRequest request);

        Task<DetectFacesResponse> ValidateSingleFace(DetectFacesRequest request);

        Task<DeleteFacesResponse> DeleteFacesAsync(string collectionId, List<string> faceIds);

        Task<(bool, float, Amazon.Rekognition.Model.BoundingBox?)> FaceMatch(byte[] originalImage, byte[] targetImage);

        Task<CompareFacesResponse> CompareFaceMatch(byte[] originalImage, byte[] targetImage);

        Task<DetectDocumentTextResponse> ExtractTextAsync(byte[] bytes);
        Task<string> EmptyBucketContents(string s3BucketName);
        Task<string> DeleteBucketAsync(string s3BucketName);
    }

    public enum CollectionStatus { Existing, Created, Failed }

    internal class AmazonApiService(IAmazonRekognition rekognitionClient, IAmazonTextract textractClient, IAmazonS3 s3Client, ILogger<AmazonApiService> logger) : IAmazonApiService
    {
        private const float similarityThreshold = 70F;
        private readonly IAmazonRekognition _rekognitionClient = rekognitionClient;
        private readonly IAmazonTextract _textractClient = textractClient;
        private readonly IAmazonS3 _s3Client = s3Client;
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
                return createdResponse.StatusCode == (int)HttpStatusCode.OK ? CollectionStatus.Created : CollectionStatus.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error ensuring collection: {ex.Message}");
            }
            return CollectionStatus.Failed;
        }

        public async Task<List<Face>> GetAllFacesFromCollectionAsync(string collectionId)
        {
            var faceList = new List<Face>();
            string nextToken = null!;

            try
            {
                var sanitizedId = collectionId?.Replace("\n", "").Replace("\r", "");

                do
                {
                    var request = new ListFacesRequest
                    {
                        CollectionId = sanitizedId,
                        NextToken = nextToken,
                        MaxResults = 4096 // You can set this up to 4096
                    };

                    var response = await _rekognitionClient.ListFacesAsync(request);

                    if (response.HttpStatusCode == HttpStatusCode.OK && response.Faces != null)
                    {
                        faceList.AddRange(response.Faces);
                    }

                    nextToken = response.NextToken;

                } while (!string.IsNullOrEmpty(nextToken));

                return faceList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Collection {collectionId} not found: {ex.Message}");
                throw; // Or return an empty list/custom response depending on your architecture
            }
        }
        public async Task<List<string>> GetAllFaceCollectionIdsAsync()
        {
            var collectionIds = new List<string>();

            try
            {
                // 1. Setup the pagination mechanism for ListCollections
                var paginator = _rekognitionClient.Paginators.ListCollections(new ListCollectionsRequest
                {
                    // You can optionally define MaxResults per page here (e.g., MaxResults = 100)
                });

                // 2. Stream through all response pages automatically
                await foreach (var response in paginator.Responses)
                {
                    if (response.CollectionIds != null)
                    {
                        collectionIds.AddRange(response.CollectionIds);
                    }
                }

                _logger.LogInformation($"Successfully retrieved {collectionIds.Count} Rekognition collection ID(s).");
            }
            catch (AmazonRekognitionException ex)
            {
                _logger.LogError($"AWS Rekognition error listing collections: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"General error listing collections: {ex.Message}");
            }

            return collectionIds;
        }
        public async Task<string> FaceCollectionExistAsync(string collectionId)
        {
            try
            {
                var sanitizedId = collectionId?.Replace("\n", "").Replace("\r", "");

                var response = await _rekognitionClient.DescribeCollectionAsync(new DescribeCollectionRequest
                {
                    CollectionId = sanitizedId
                });

                if (response.HttpStatusCode == HttpStatusCode.OK && response.FaceCount > 0)
                {
                    _logger.LogInformation($"Collection [{sanitizedId}] exists with {response.FaceCount} faces.");
                    return $"Collection [{sanitizedId}] exists with {response.FaceCount} faces";
                }
                else
                {
                    _logger.LogInformation($"Collection [{sanitizedId}] does not exist or is empty.");
                    return $"Collection [{sanitizedId}] does not exist or is empty";
                }
            }
            catch (Amazon.Rekognition.Model.ResourceNotFoundException ex)
            {
                _logger.LogError($"Error ensuring collection: {ex.Message}");
                return $"Collection [{collectionId}] does not exist or is empty";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error ensuring collection: {ex.Message}");
                return $"Collection [{collectionId}] does not exist or is empty";
            }
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
                if (response.HttpStatusCode == HttpStatusCode.OK && response.FaceCount > 0)
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
                    return new DeleteCollectionResponse { StatusCode = (int)HttpStatusCode.NotFound };
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
                return new DeleteCollectionResponse { StatusCode = (int)HttpStatusCode.InternalServerError };
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

        public async Task<DetectFacesResponse> ValidateSingleFace(DetectFacesRequest request)
        {
            return await _rekognitionClient.DetectFacesAsync(request);
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

        public async Task<string> DeleteBucketAsync(string s3BucketName)
        {
            try
            {
                bool bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, s3BucketName);
                if (!bucketExists)
                {
                    return $"Bucket {s3BucketName} does not exist.";
                }
                var locationRequest = new GetBucketLocationRequest
                {
                    BucketName = s3BucketName
                };
                GetBucketLocationResponse locationResponse = await _s3Client.GetBucketLocationAsync(locationRequest);

                string locationConstraint = locationResponse.Location?.Value!;
                var regionName = string.IsNullOrEmpty(locationConstraint) || locationConstraint == "US" ? "us-east-1" : locationConstraint;

                var bucketRegion = RegionEndpoint.GetBySystemName(regionName);

                using var regionalClient = new AmazonS3Client(EnvHelper.Get("AWS_ID"), EnvHelper.Get("AWS_SECRET"), bucketRegion);

                await EmptyBucketForAnyRegionAsync(s3BucketName, regionalClient);

                var deletedResponse = await regionalClient.DeleteBucketAsync(s3BucketName);
                return deletedResponse.HttpStatusCode == HttpStatusCode.NoContent ? $"Bucket [{s3BucketName}] deleted successfully." : $"Failed to delete bucket [{s3BucketName}].";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete bucket {s3BucketName}");
                return $"Failed to delete bucket [{s3BucketName}]: {ex.Message}";
            }
        }

        public async Task<string> EmptyBucketContents(string s3BucketName)
        {
            try
            {
                bool bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, s3BucketName);
                if (!bucketExists)
                {
                    return $"Bucket [{s3BucketName}] does not exist.";
                }
                await EmptyBucketAsync(s3BucketName);
                return $"Bucket [{s3BucketName}] emptied successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to empty bucket {s3BucketName}");
                return $"Failed to empty bucket {s3BucketName}: {ex.Message}";
            }
        }

        private async Task EmptyBucketForAnyRegionAsync(string bucketName, IAmazonS3 s3Client)
        {
            var request = new ListObjectsV2Request { BucketName = bucketName };
            ListObjectsV2Response response;

            do
            {
                response = await s3Client.ListObjectsV2Async(request);

                if (response.S3Objects.Any())
                {
                    var keysToDelete = response.S3Objects
                        .Select(obj => new KeyVersion { Key = obj.Key })
                        .ToList();

                    await s3Client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = bucketName,
                        Objects = keysToDelete
                    });
                }

                request.ContinuationToken = response.NextContinuationToken;

            } while (response.IsTruncated ?? false);
        }
        private async Task EmptyBucketAsync(string bucketName)
        {
            var request = new ListObjectsV2Request { BucketName = bucketName };
            ListObjectsV2Response response;

            do
            {
                response = await _s3Client.ListObjectsV2Async(request);

                if (response.S3Objects.Any())
                {
                    var keysToDelete = response.S3Objects
                        .Select(obj => new KeyVersion { Key = obj.Key })
                        .ToList();

                    await _s3Client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = bucketName,
                        Objects = keysToDelete
                    });
                }

                request.ContinuationToken = response.NextContinuationToken;

            } while (response.IsTruncated ?? false);
        }
    }
}