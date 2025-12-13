using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace risk.control.system.Services
{
    public interface ICompareFaces
    {
        Task<(bool, float, Amazon.Rekognition.Model.BoundingBox?)> DoFaceMatch(byte[] data, byte[] tdata);
    }
    internal class CompareFaces : ICompareFaces
    {
        private readonly IAmazonRekognition rekognitionClient;
        private readonly IAmazonTextract textractClient;
        private readonly ILogger<ComparedFace> logger;

        public CompareFaces(IAmazonRekognition rekognitionClient, IAmazonTextract textractClient, ILogger<ComparedFace> logger)
        {
            this.rekognitionClient = rekognitionClient;
            this.textractClient = textractClient;
            this.logger = logger;
        }
        public async Task<(bool, float, Amazon.Rekognition.Model.BoundingBox?)> DoFaceMatch(byte[] data, byte[] tdata)
        {
            float similarityThreshold = 70F;
            Image imageSource = new();
            Image imageTarget = new();

            try
            {
                imageSource.Bytes = new MemoryStream(data);
                imageTarget.Bytes = new MemoryStream(tdata);

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
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine($"Failed to load source image:");
                logger.LogError(ex, "Failed to compare faces");
                return (false, 0, null);
            }
        }

        public async Task DetectSampleAsync(byte[] bytes)
        {
            Console.WriteLine("Detect Document Text");
            var detectResponse = await textractClient.DetectDocumentTextAsync(new DetectDocumentTextRequest
            {
                Document = new Document
                {
                    Bytes = new MemoryStream(bytes)
                }
            });

            foreach (var block in detectResponse.Blocks)
            {
                Console.WriteLine($"Type {block.BlockType}, Text: {block.Text}");
            }
        }
    }
}
