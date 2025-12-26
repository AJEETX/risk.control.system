using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Helpers
{
    public class AgentVerificationHelper
    {
        public static async Task<byte[]> GetBytesFromIFormFile(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return ms.ToArray();
        }

        public static (string lat, string lon) ParseCoordinates(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || !raw.Contains('/'))
                return ("0", "0");

            var parts = raw.Split('/');
            return (parts[0].Trim(), parts[1].Trim().Replace("/", ""));
        }

        public static (double lat, double lon) GetExpectedCoordinates(InvestigationTask claim)
        {
            bool isClaim = claim.PolicyDetail?.InsuranceType == InsuranceType.CLAIM;
            var latStr = isClaim ? claim.BeneficiaryDetail?.Latitude : claim.CustomerDetail?.Latitude;
            var lonStr = isClaim ? claim.BeneficiaryDetail?.Longitude : claim.CustomerDetail?.Longitude;

            double.TryParse(latStr, out double lat);
            double.TryParse(lonStr, out double lon);

            return (lat, lon);
        }

        public static void MapMetadataToReports(AgentIdReport face, LocationReport loc, FaceData data, string path, string name, string lat, string lon)
        {
            face.FilePath = path;
            face.ImageExtension = Path.GetExtension(name);
            face.Updated = DateTime.Now;
            face.UpdatedBy = data.Email;
            face.LongLatTime = DateTime.Now;
            face.LongLat = $"Latitude = {lat}, Longitude = {lon}";
            face.ValidationExecuted = true;

            loc.Updated = DateTime.Now;
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
