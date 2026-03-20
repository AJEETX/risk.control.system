namespace risk.control.system.Services.Agent
{
    public interface IFaceMatchService
    {
        Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes, string onlyExtension);
    }

    internal class FaceMatchService : IFaceMatchService
    {
        private readonly IAmazonApiService compareFaces;
        private readonly IProcessImageService processImageService;
        private readonly ILogger<FaceMatchService> logger;

        public FaceMatchService(IAmazonApiService compareFaces, IProcessImageService processImageService, ILogger<FaceMatchService> logger)
        {
            this.compareFaces = compareFaces;
            this.processImageService = processImageService;
            this.logger = logger;
        }

        public async Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes, string onlyExtension)
        {
            try
            {
                var matched = await compareFaces.FaceMatch(registeredImage, faceImageBytes);
                return matched.Item1 ? (matched.Item2.ToString(), processImageService.CompressImage(faceImageBytes), matched.Item2) : ("0", processImageService.CompressImage(faceImageBytes), 0);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed face match");
                return ("0", processImageService.CompressImage(faceImageBytes), 0);
            }
        }
    }
}