using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Helpers
{
    public static class FaceIdfyHelper
    {
        public static string GetRegisteredImagePath(InvestigationTask claim, bool isCustomerVerification)
        {
            return isCustomerVerification
                ? claim.CustomerDetail.ImagePath
                : claim.BeneficiaryDetail.ImagePath;
        }

        public static void MapMetadataToReport(FaceIdReport face, LocationReport loc, FaceData data, string path, string ext, string lat, string lon)
        {
            face.FilePath = path;
            face.ImageExtension = ext;
            face.Updated = DateTime.UtcNow;
            face.UpdatedBy = data.Email;
            face.LongLatTime = DateTime.UtcNow;
            face.LongLat = $"Latitude = {lat}, Longitude = {lon}";
            face.ValidationExecuted = true;

            loc.AgentEmail = data.Email;
            loc.Updated = DateTime.UtcNow;
            loc.ValidationExecuted = true;
        }

        public static AppiCheckifyResponse BuildResponse(InvestigationTask claim, FaceIdReport face, byte[] img)
        {
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
                Image = img,
                LocationImage = face.FilePath,
                LocationLongLat = face.LongLat,
                LocationTime = face.LongLatTime,
                FacePercent = face.MatchConfidence
            };
        }
    }
}