using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class IdfyHelper
    {
        public static (string lat, string lon) ParseCoordinates(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || !raw.Contains('/')) return ("0", "0");
            var parts = raw.Split('/');
            return (parts[0].Trim(), parts[1].Trim().Replace("/", ""));
        }

        public static (double lat, double lon) GetExpectedCoordinates(InvestigationTask claim)
        {
            bool isUnderwriting = claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING;
            var lat = isUnderwriting ? claim.CustomerDetail.Latitude : claim.BeneficiaryDetail.Latitude;
            var lon = isUnderwriting ? claim.CustomerDetail.Longitude : claim.BeneficiaryDetail.Longitude;

            double.TryParse(lat, out double dLat);
            double.TryParse(lon, out double dLon);
            return (dLat, dLon);
        }

        public static async Task<byte[]> ToByteArrayAsync(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}
