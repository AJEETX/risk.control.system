using Google.Cloud.Vision.V1;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace risk.control.system.Helpers
{
    public interface IGoogleApi
    {
        Task<IReadOnlyList<EntityAnnotation>> DetectTextAsync(byte[] byteImage);
    }
    public class GoogleApi : IGoogleApi
    {
        private readonly IWebHostEnvironment webHostEnvironment;

        public GoogleApi(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }
        public async Task<IReadOnlyList<EntityAnnotation>> DetectTextAsync(byte[] byteImage)
        {
            var cred = Path.Combine(webHostEnvironment.WebRootPath, "icheckify-111c8d2c04f0.json");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", cred);

            var client = ImageAnnotatorClient.Create();
            var image = Image.FromBytes(byteImage);
            try
            {
                return await client.DetectTextAsync(image);

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
