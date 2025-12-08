using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;
namespace risk.control.system.Services
{
    public interface IGoogleApi
    {
        Task<IReadOnlyList<EntityAnnotation>> DetectTextAsync(string imagePath);
    }
    internal class GoogleApi : IGoogleApi
    {
        private readonly IWebHostEnvironment webHostEnvironment;

        public GoogleApi(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
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
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
