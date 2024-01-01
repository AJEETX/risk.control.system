using Microsoft.AspNetCore.Hosting;

using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IClaimsVendorService
    {
        Task<AppiCheckifyResponse> PostFaceId(string userEmail, string claimId);

        Task<AppiCheckifyResponse> PostDocumentId(string userEmail, string claimId);
    }

    public class ClaimsVendorService : IClaimsVendorService
    {
        private readonly IICheckifyService checkifyService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private static string latitude = "-37.839542";
        private static string longitude = "145.164834";

        public ClaimsVendorService(IICheckifyService checkifyService, IWebHostEnvironment webHostEnvironment)
        {
            this.checkifyService = checkifyService;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<AppiCheckifyResponse> PostDocumentId(string userEmail, string claimId)
        {
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "agency", "pan.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);

            var data = new DocumentData
            {
                Email = userEmail,
                ClaimId = claimId,
                OcrImage = Convert.ToBase64String(noDataimage),
                OcrLongLat = $"{latitude}/{longitude}"
            };
            var result = await checkifyService.GetDocumentId(data);
            return result;
        }

        public async Task<AppiCheckifyResponse> PostFaceId(string userEmail, string claimId)
        {
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "agency", "ajeet.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);

            var data = new FaceData
            {
                Email = userEmail,
                ClaimId = claimId,
                LocationImage = Convert.ToBase64String(noDataimage),
                LocationLongLat = $"{latitude}/{longitude}"
            };

            var result = await checkifyService.GetFaceId(data);
            return result;
        }
    }
}