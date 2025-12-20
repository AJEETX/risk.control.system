using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public class LocationDetail
    {
        public static string GetAddress(bool claimType, CustomerDetail a, BeneficiaryDetail location)
        {
            if (claimType)
            {
                if (a is null)
                    return string.Empty;
                return a.Addressline + " " + a.District?.Name + " " + a.State?.Name + " " + a.Country?.Name + " " + a.PinCode?.Code;
            }
            else
            {
                if (location is null)
                    return string.Empty;
                return location.Addressline + " " + location.District.Name + " " + location.State.Name + " " + location.Country.Name + " " + location.PinCode.Code;
            }
        }
    }
}