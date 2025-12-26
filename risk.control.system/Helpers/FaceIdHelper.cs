using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Helpers
{
    public static class FaceIdHelper
    {
        public static (string lat, string lon) ParseCoordinates(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || !raw.Contains('/')) return ("0", "0");
            var parts = raw.Split('/');
            return (parts[0].Trim(), parts[1].Trim().Replace("/", ""));
        }

        public static string GetRegisteredImagePath(InvestigationTask claim, bool isCustomerVerification)
        {
            return isCustomerVerification
                ? claim.CustomerDetail.ImagePath
                : claim.BeneficiaryDetail.ImagePath;
        }

        public static (double lat, double lon) GetExpectedCoordinates(InvestigationTask claim)
        {
            bool isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;
            var lat = isClaim ? claim.BeneficiaryDetail.Latitude : claim.CustomerDetail.Latitude;
            var lon = isClaim ? claim.BeneficiaryDetail.Longitude : claim.CustomerDetail.Longitude;
            return (double.Parse(lat), double.Parse(lon));
        }

        public static void MapMetadata(FaceIdReport face, LocationReport loc, FaceData data, string path, string ext, string lat, string lon)
        {
            face.FilePath = path;
            face.ImageExtension = ext;
            face.Updated = DateTime.Now;
            face.UpdatedBy = data.Email;
            face.LongLatTime = DateTime.Now;
            face.LongLat = $"Latitude = {lat}, Longitude = {lon}";
            face.ValidationExecuted = true;

            loc.AgentEmail = data.Email;
            loc.Updated = DateTime.Now;
            loc.ValidationExecuted = true;
        }
    }
}
