namespace risk.control.system.Services.Agent
{
    public interface IFaceMatchService
    {
        Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes, string onlyExtension);
    }

    internal class FaceMatchService(IAmazonApiService compareFaces, IProcessImageService processImageService, ILogger<FaceMatchService> logger) : IFaceMatchService
    {
        private readonly IAmazonApiService _compareFaces = compareFaces;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly ILogger<FaceMatchService> _logger = logger;

        public async Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes, string onlyExtension)
        {
            try
            {
                var matched = await _compareFaces.FaceMatch(registeredImage, faceImageBytes);
                return matched.Item1 ? (matched.Item2.ToString(), _processImageService.CompressImage(faceImageBytes), matched.Item2) : ("0", _processImageService.CompressImage(faceImageBytes), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed face match");
                return ("0", _processImageService.CompressImage(faceImageBytes), 0);
            }
        }
    }
}