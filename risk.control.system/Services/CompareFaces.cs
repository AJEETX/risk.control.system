using Amazon.Rekognition.Model;
using Amazon.Rekognition;
using Amazon;
using risk.control.system.AppConstant;
using Amazon.Textract.Model;
using Amazon.Textract;

namespace risk.control.system.Services
{
    public interface ICompareFaces
    {
        Task<(bool, float)> Do(byte[] data, byte[] tdata);
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
        public async Task<(bool, float)> Do(byte[] data, byte[] tdata)
        {
            float similarityThreshold = 70F;
            
            Amazon.Rekognition.Model.Image imageSource = new Amazon.Rekognition.Model.Image();

            try
            {

                imageSource.Bytes = new MemoryStream(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine($"Failed to load source image:");
                return (false, 0);
            }

            Amazon.Rekognition.Model.Image imageTarget = new Amazon.Rekognition.Model.Image();

            try
            {

                imageTarget.Bytes = new MemoryStream(tdata);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine($"Failed to load source image:");
                return (false, 0);
            }

            var compareFacesRequest = new CompareFacesRequest
            {
                SourceImage = imageSource,
                TargetImage = imageTarget,
                SimilarityThreshold = similarityThreshold,
            };

            // Call operation
            var compareFacesResponse = await rekognitionClient.CompareFacesAsync(compareFacesRequest);

            var result = compareFacesResponse.FaceMatches.Count == 1 && compareFacesResponse.UnmatchedFaces.Count == 0 && compareFacesResponse.FaceMatches[0].Similarity >= similarityThreshold;

            //// Display results
            //compareFacesResponse.FaceMatches.ForEach(match =>
            //{
            //    ComparedFace face = match.Face;
            //    BoundingBox position = face.BoundingBox;
            //    Console.WriteLine($"Face at {position.Left} {position.Top} matches with {match.Similarity}% confidence.");
            //});

            //Console.WriteLine($"Found {compareFacesResponse.UnmatchedFaces.Count} face(s) that did not match.");
            return (result, compareFacesResponse.FaceMatches[0].Similarity);
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
