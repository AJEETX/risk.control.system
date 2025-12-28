using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;
namespace risk.control.system.Services
{
    public interface IGoogleService
    {
        Task<IReadOnlyList<EntityAnnotation>> DetectTextAsync(string imagePath);
    }
    internal class GoogleService : IGoogleService
    {
        private readonly ILogger<GoogleService> logger;

        public GoogleService(ILogger<GoogleService> logger)
        {
            this.logger = logger;
        }
        public async Task<IReadOnlyList<EntityAnnotation>> DetectTextAsync(string imagePath)
        {
            try
            {
                string credentialJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");

                // Create Google Credential from JSON string
                GoogleCredential googleCredential = GoogleCredential.FromJson(credentialJson);

                var client = new ImageAnnotatorClientBuilder
                {
                    Credential = googleCredential
                }.Build();

                var image = Image.FromFile(imagePath);
                return await client.DetectTextAsync(image);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                return null!;
            }
        }
    }
}
