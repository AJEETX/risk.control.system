using System.Text;
using Amazon.Rekognition.Model;
using Amazon.Textract.Model;
using risk.control.system.Services.Agent;
using BoundingBox = Amazon.Rekognition.Model.BoundingBox;

namespace risk.control.system.Services.Fakes
{
    /// <summary>
    /// Development/E2E stand-in for AWS Rekognition, Textract and S3. Registered only when
    /// USE_FAKE_EXTERNAL_SERVICES=true in Development (see FakeExternalServiceExtension).
    ///
    /// Every image is treated as a clear photo of one person that matches the registered face, unless the image
    /// bytes contain the ASCII marker <see cref="NoMatchMarker"/> (tests append it after the JPEG end marker,
    /// which image decoders ignore). Then the face does not match.
    /// </summary>
    internal sealed class FakeAmazonApiService(ILogger<FakeAmazonApiService> logger) : IAmazonApiService
    {
        public const string NoMatchMarker = "E2E-NOMATCH";
        private const float MatchSimilarity = 98.5f;

        private readonly ILogger<FakeAmazonApiService> _logger = logger;

        public Task<(bool, float, BoundingBox?)> FaceMatch(byte[] originalImage, byte[] targetImage)
        {
            var matched = !HasNoMatchMarker(targetImage);
            _logger.LogInformation("[FAKE AWS] FaceMatch -> {Matched}", matched);
            return Task.FromResult<(bool, float, BoundingBox?)>(matched
                ? (true, MatchSimilarity, FaceBox())
                : (false, 0f, null));
        }

        public Task<CompareFacesResponse> CompareFaceMatch(byte[] originalImage, byte[] targetImage)
        {
            var response = new CompareFacesResponse
            {
                FaceMatches = new List<CompareFacesMatch>(),
                UnmatchedFaces = new List<ComparedFace>(),
            };

            if (HasNoMatchMarker(targetImage))
            {
                response.UnmatchedFaces.Add(new ComparedFace { BoundingBox = FaceBox(), Confidence = 99.9f });
            }
            else
            {
                response.FaceMatches.Add(new CompareFacesMatch
                {
                    Similarity = MatchSimilarity,
                    Face = new ComparedFace { BoundingBox = FaceBox(), Confidence = 99.9f },
                });
            }

            return Task.FromResult(response);
        }

        public Task<DetectFacesResponse> ValidateSingleFace(DetectFacesRequest request)
        {
            return Task.FromResult(new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail>
                {
                    new()
                    {
                        Confidence = 99.9f,
                        BoundingBox = FaceBox(),
                        AgeRange = new AgeRange { Low = 30, High = 40 },
                        Gender = new Gender { Value = "Male", Confidence = 99.1f },
                        Emotions = new List<Emotion> { new() { Type = "CALM", Confidence = 95.2f } },
                        Smile = new Smile { Value = false, Confidence = 90f },
                        Beard = new Beard { Value = false, Confidence = 90f },
                        Eyeglasses = new Eyeglasses { Value = false, Confidence = 90f },
                    },
                },
            });
        }

        public Task<DetectDocumentTextResponse> ExtractTextAsync(byte[] bytes)
        {
            var lines = FakeOcrText.Lines;
            var blocks = lines.Select((line, i) => new Block
            {
                BlockType = "LINE",
                Text = line,
                Confidence = 99f,
                Geometry = new Amazon.Textract.Model.Geometry
                {
                    BoundingBox = new Amazon.Textract.Model.BoundingBox { Left = 0.1f, Top = 0.1f + (i * 0.08f), Width = 0.6f, Height = 0.06f },
                },
            }).ToList();

            return Task.FromResult(new DetectDocumentTextResponse { Blocks = blocks });
        }

        // Face collections (startup migration and duplicate-face checks): behave as an empty collection.
        public Task<CollectionStatus> EnsureCollectionExistsAsync(string collectionId) =>
            Task.FromResult(CollectionStatus.Existing);

        public Task<List<string>> GetAllFaceCollectionIdsAsync() => Task.FromResult(new List<string>());

        public Task<string> FaceCollectionExistAsync(string collectionId) => Task.FromResult(collectionId);

        public Task<List<Face>> GetAllFacesFromCollectionAsync(string collectionId) => Task.FromResult(new List<Face>());

        public Task<DeleteCollectionResponse> DeleteCollectionAsync(string collectionId) =>
            Task.FromResult(new DeleteCollectionResponse { StatusCode = 200 });

        public Task<IndexFacesResponse> IndexFacesAsync(IndexFacesRequest request) =>
            Task.FromResult(new IndexFacesResponse { FaceRecords = new List<FaceRecord>(), UnindexedFaces = new List<UnindexedFace>() });

        public Task<SearchFacesByImageResponse> SearchFacesByImageAsync(SearchFacesByImageRequest request) =>
            Task.FromResult(new SearchFacesByImageResponse { FaceMatches = new List<FaceMatch>() });

        public Task<DeleteFacesResponse> DeleteFacesAsync(string collectionId, List<string> faceIds) =>
            Task.FromResult(new DeleteFacesResponse { DeletedFaces = faceIds });

        // S3 housekeeping
        public Task<string> EmptyBucketContents(string s3BucketName) => Task.FromResult("OK");

        public Task<string> DeleteBucketAsync(string s3BucketName) => Task.FromResult("OK");

        public Task<string> DeleteS3FolderAsync(string s3BucketName, string folderPath) => Task.FromResult("OK");

        public Task<string> DeleteSingleFileFromBucketAsync(string s3BucketName, string keyName) => Task.FromResult("OK");

        private static BoundingBox FaceBox() => new() { Left = 0.3f, Top = 0.2f, Width = 0.4f, Height = 0.5f };

        internal static bool HasNoMatchMarker(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            var marker = Encoding.ASCII.GetBytes(NoMatchMarker);
            return bytes.AsSpan().IndexOf(marker) >= 0;
        }
    }
}
