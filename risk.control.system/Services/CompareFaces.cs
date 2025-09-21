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
    public class CompareFaces : ICompareFaces
    {
        private readonly IAmazonRekognition rekognitionClient;
        private readonly IAmazonTextract textractClient;

        public CompareFaces(IAmazonRekognition rekognitionClient, IAmazonTextract textractClient)
        {
            this.rekognitionClient = rekognitionClient;
            this.textractClient = textractClient;
        }
        public async Task<(bool, float, Amazon.Rekognition.Model.BoundingBox?)> DoFaceMatch(byte[] data, byte[] tdata)
        {
            float similarityThreshold = 70F;

            Image imageSource = new();

            try
            {

                imageSource.Bytes = new MemoryStream(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine($"Failed to load source image:");
                return (false, 0, null);
            }

            Image imageTarget = new();

            try
            {

                imageTarget.Bytes = new MemoryStream(tdata);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine($"Failed to load source image:");
                return (false, 0, null);
            }

            var compareFacesRequest = new CompareFacesRequest
            {
                SourceImage = imageSource,
                TargetImage = imageTarget,
                SimilarityThreshold = similarityThreshold,
            };

            // Call operation
            var compareFacesResponse = await rekognitionClient.CompareFacesAsync(compareFacesRequest);

            var result = compareFacesResponse.FaceMatches.Count >= 1
                //&& compareFacesResponse.UnmatchedFaces.Count == 0 
                && compareFacesResponse.FaceMatches[0].Similarity >= similarityThreshold;
            var faceBox = result ? compareFacesResponse.FaceMatches[0].Face.BoundingBox : compareFacesResponse.UnmatchedFaces[0].BoundingBox;
            var similarity = result ? compareFacesResponse.FaceMatches[0].Similarity.GetValueOrDefault() : 0;
            //// Display results
            //compareFacesResponse.FaceMatches.ForEach(match =>
            //{
            //    ComparedFace face = match.Face;
            //    BoundingBox position = face.BoundingBox;
            //    Console.WriteLine($"Face at {position.Left} {position.Top} matches with {match.Similarity}% confidence.");
            //});

            //Console.WriteLine($"Found {compareFacesResponse.UnmatchedFaces.Count} face(s) that did not match.");
            return (result, similarity, faceBox);
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
