using Highsoft.Web.Mvc.Charts;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IFaceMatchService
    {
        Task<(string, byte[])> GetFaceMatchAsync(byte[] registeredImage, string faceImage);
    }
    public class FaceMatchService : IFaceMatchService
    {
        public async Task<(string, byte[])> GetFaceMatchAsync(byte[] registeredImage, string faceImage)
        {
            string ImageData = string.Empty;
            try
            {
                var face2Verify = Convert.FromBase64String(faceImage);
                var matched = await CompareFaces.Do(registeredImage, face2Verify);
                return matched ? ("99", CompressImage.ProcessCompress(face2Verify)) : (string.Empty, CompressImage.ProcessCompress(face2Verify));
            }
            catch (Exception ex)
            {
                return (string.Empty, CompressImage.ProcessCompress(registeredImage));
            }
        }
    }
}
