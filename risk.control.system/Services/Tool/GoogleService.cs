using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using Image = Google.Cloud.Vision.V1.Image;

namespace risk.control.system.Services.Tool
{
    public interface IGoogleService
    {
        Task<IReadOnlyList<EntityAnnotation>> DetectTextAsync(string imagePath);
        Task<IReadOnlyList<TextBlock>> DetectText(string imagePath);
    }

    internal class GoogleService : IGoogleService
    {
        private readonly ILogger<GoogleService> logger;

        public GoogleService(ILogger<GoogleService> logger)
        {
            this.logger = logger;
        }

        public async Task<IReadOnlyList<TextBlock>> DetectText(string imagePath)
        {
            try
            {
                string credentialJson = EnvHelper.Get("GOOGLE_APPLICATION_CREDENTIALS_JSON")!; // "{ \"type\": \"service_account\", ... }";

                // Create Google Credential from JSON string
                var googleCredential = CredentialFactory.FromJson<ServiceAccountCredential>(credentialJson).ToGoogleCredential();

                var client = new ImageAnnotatorClientBuilder
                {
                    Credential = googleCredential
                }.Build();

                var response = await client.DetectTextAsync(Image.FromFile(imagePath));

                // Map Google vertices to our generic TextBlock
                return [.. response.Select(t => new TextBlock(
                    t.Description,
                    t.BoundingPoly.Vertices[0].X, // Left
                    t.BoundingPoly.Vertices[0].Y, // Top
                    t.BoundingPoly.Vertices[2].X, // Right
                    t.BoundingPoly.Vertices[2].Y  // Bottom
                ))];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred While text detection.");
                return null!;
            }
        }

        public async Task<IReadOnlyList<EntityAnnotation>> DetectTextAsync(string imagePath)
        {
            try
            {
                string credentialJson = EnvHelper.Get("GOOGLE_APPLICATION_CREDENTIALS_JSON")!; // "{ \"type\": \"service_account\", ... }";

                // Create Google Credential from JSON string
                var googleCredential = CredentialFactory.FromJson<ServiceAccountCredential>(credentialJson).ToGoogleCredential();

                var client = new ImageAnnotatorClientBuilder
                {
                    Credential = googleCredential
                }.Build();

                var image = Image.FromFile(imagePath);
                return await client.DetectTextAsync(image);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred While text detection.");
                return null!;
            }
        }
    }
}