using Highsoft.Web.Mvc.Charts;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IFaceMatchService
    {
        Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, string faceImage);
    }
    public class FaceMatchService : IFaceMatchService
    {
        private readonly ICompareFaces compareFaces;

        public FaceMatchService(ICompareFaces compareFaces)
        {
            this.compareFaces = compareFaces;
        }
        public async Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, string faceImage)
        {
            string ImageData = string.Empty;
            byte[] face2Verify = null;
            try
            {
                face2Verify = Convert.FromBase64String(faceImage);
                var matched = await compareFaces.Do(registeredImage, face2Verify);
                return matched.Item1 ? (matched.Item2.ToString(), CompressImage.ProcessCompress(face2Verify), matched.Item2) : ("0", CompressImage.ProcessCompress(face2Verify),0);
            }
            catch (Exception ex)
            {
                return ("0", CompressImage.ProcessCompress(face2Verify),0);
            }
        }
    }
}
