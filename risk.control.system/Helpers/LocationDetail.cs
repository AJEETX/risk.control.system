using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class LocationDetail
    {
        public static string GetAddress(bool claimType, CustomerDetail customer, BeneficiaryDetail beneficiary)
        {
            if (claimType)
            {
                if (customer is null)
                    return string.Empty;
                return customer.Addressline + " " + customer.District?.Name + " " + customer.State?.Name + " " + customer.Country?.Name + " " + customer.PinCode?.Code;
            }
            else
            {
                if (beneficiary is null)
                    return string.Empty;
                return beneficiary.Addressline + " " + beneficiary.District.Name + " " + beneficiary.State.Name + " " + beneficiary.Country.Name + " " + beneficiary.PinCode.Code;
            }
        }
    }
}