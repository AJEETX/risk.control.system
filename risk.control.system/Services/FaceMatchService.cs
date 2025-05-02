using Highsoft.Web.Mvc.Charts;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IFaceMatchService
    {
        Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] face2Verify);
    }
    public class FaceMatchService : IFaceMatchService
    {
        private readonly ICompareFaces compareFaces;

        public FaceMatchService(ICompareFaces compareFaces)
        {
            this.compareFaces = compareFaces;
        }
        public async Task<(string, byte[], float)> GetFaceMatchAsync(byte[] registeredImage, byte[] face2Verify)
        {
            string ImageData = string.Empty;
            try
            {
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
