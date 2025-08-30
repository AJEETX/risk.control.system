using risk.control.system.Helpers;

namespace risk.control.system.Services
{
    public interface IFaceMatchService
    {
        Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes, string onlyExtension);
    }
    public class FaceMatchService : IFaceMatchService
    {
        private readonly ICompareFaces compareFaces;

        public FaceMatchService(ICompareFaces compareFaces)
        {
            this.compareFaces = compareFaces;
        }
        public async Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] faceImageBytes, string onlyExtension)
        {
            string ImageData = string.Empty;
            try
            {
                var matched = await compareFaces.DoFaceMatch(registeredImage, faceImageBytes);
                return matched.Item1 ? (matched.Item2.ToString(), CompressImage.ProcessCompress(faceImageBytes, onlyExtension, 10, 99, matched.Item3), matched.Item2) : ("0", CompressImage.ProcessCompress(faceImageBytes, onlyExtension, 10, 99, matched.Item3), 0);
            }
            catch (Exception)
            {
                return ("0", CompressImage.ProcessCompress(faceImageBytes, onlyExtension), 0);
            }
        }
    }
}
