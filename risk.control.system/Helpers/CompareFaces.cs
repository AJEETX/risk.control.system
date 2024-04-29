using Amazon.Rekognition.Model;
using Amazon.Rekognition;
using Amazon;
using risk.control.system.AppConstant;

namespace risk.control.system.Services
{
    public class CompareFaces
    {
        public static async Task<bool> Do(byte[] data, byte[] tdata)
        {
            float similarityThreshold = 70F;
            var awsAccessKeyId = Environment.GetEnvironmentVariable("aws_id") ?? Applicationsettings.aws_id;
            var awsSecretAccessKey = Environment.GetEnvironmentVariable("aws_secret") ?? Applicationsettings.aws_secret;
            var rekognitionClient = new AmazonRekognitionClient(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.APSoutheast2);

            Amazon.Rekognition.Model.Image imageSource = new Amazon.Rekognition.Model.Image();

            try
            {

                imageSource.Bytes = new MemoryStream(data);
            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to load source image:");
                return false;
            }

            Amazon.Rekognition.Model.Image imageTarget = new Amazon.Rekognition.Model.Image();

            try
            {

                imageTarget.Bytes = new MemoryStream(tdata);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load source image:");
                Console.WriteLine(ex.Message);
                return false;
            }

            var compareFacesRequest = new CompareFacesRequest
            {
                SourceImage = imageSource,
                TargetImage = imageTarget,
                SimilarityThreshold = similarityThreshold,
            };

            // Call operation
            var compareFacesResponse = await rekognitionClient.CompareFacesAsync(compareFacesRequest);

            var result = compareFacesResponse.FaceMatches.Count > 0 && compareFacesResponse.UnmatchedFaces.Count == 0;
            // Display results
            compareFacesResponse.FaceMatches.ForEach(match =>
            {
                ComparedFace face = match.Face;
                BoundingBox position = face.BoundingBox;
                Console.WriteLine($"Face at {position.Left} {position.Top} matches with {match.Similarity}% confidence.");
            });

            Console.WriteLine($"Found {compareFacesResponse.UnmatchedFaces.Count} face(s) that did not match.");
            return result;
        }
    }
}
