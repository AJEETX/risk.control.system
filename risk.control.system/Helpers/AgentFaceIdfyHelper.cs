using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Helpers
{
    public static class AgentFaceIdfyHelper
    {
        public static void MapMetadataToReport(AgentIdReport face, LocationReport loc, FaceData data, string path, string name, string lat, string lon)
        {
            face.FilePath = path;
            face.ImageExtension = Path.GetExtension(name);
            face.Updated = DateTime.UtcNow;
            face.UpdatedBy = data.Email;
            face.LongLatTime = DateTime.UtcNow;
            face.LongLat = $"Latitude = {lat}, Longitude = {lon}";
            face.ValidationExecuted = true;

            loc.Updated = DateTime.UtcNow;
            loc.AgentEmail = data.Email;
            loc.ValidationExecuted = true;
        }

        public static AppiCheckifyResponse CreateResponse(InvestigationTask claim, AgentIdReport face, byte[] image)
        {
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claim.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
                Image = image,
                LocationImage = face.FilePath,
                LocationLongLat = face.LongLat,
                LocationTime = face.LongLatTime,
                FacePercent = face.DigitalIdImageMatchConfidence
            };
        }
    }
}