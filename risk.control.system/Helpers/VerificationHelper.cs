using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class VerificationHelper
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
    }
}