using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class ClaimsInvestigationExtension
    {
        public static string GetPincodeOfInterest(bool claimType, int? customerPinCode, int? beneficiaryPinCode)
        {
            if (claimType)
            {
                if (customerPinCode == null || customerPinCode < 999)
                    return "<span class=\"badge badge-danger\"> <i class=\"fas fa-question\" ></i>  </span>";
                return string.Join("", "<span class='badge badge-light'>" + customerPinCode + "</span>");
            }
            else
            {
                if (beneficiaryPinCode == null || beneficiaryPinCode < 999)
                    return "<span class=\"badge badge-danger\"> <i class=\"fas fa-question\" ></i>  </span>";
                return string.Join("", "<span class='badge badge-light'>" + beneficiaryPinCode + "</span>");
            }
        }

        public static string GetPincode(bool claimType, CustomerDetail customer, BeneficiaryDetail beneficiary)
        {
            if (claimType)
            {
                if (customer is null)
                    return "<span class=\"badge badge-danger\"> <i class=\"fas fa-question\" ></i>  </span>";
                return string.Join("", "<span class='badge badge-light'>" + customer.PinCode.Code + "</span>");
            }
            else
            {
                if (beneficiary is null)
                    return "<span class=\"badge badge-danger\"> <i class=\"fas fa-question\" ></i>  </span>";
                return string.Join("", "<span class='badge badge-light'>" + beneficiary.PinCode.Code + "</span>");
            }
        }

        public static string GetPersonPhoto(bool claimType, CustomerDetail customer, BeneficiaryDetail beneficiary)
        {
            if (claimType)
            {
                if (customer is not null)
                {
                    return (customer.ImagePath);
                }
            }
            else
            {
                if (beneficiary is not null)
                {
                    return beneficiary.ImagePath;
                }
            }
            return Applicationsettings.NO_USER;
        }

        public static string GetPincodeName(bool claimType, CustomerDetail customer, BeneficiaryDetail beneficiary)
        {
            if (claimType)
            {
                if (customer is null)
                    return "...";
                return customer.Addressline + "," + customer.District?.Name + ", " + customer.State.Name + ", " + customer.PinCode.Code;
            }
            else
            {
                if (beneficiary is null)
                    return "...";
                return beneficiary.Addressline + "," + beneficiary.District.Name + ", " + beneficiary.State.Name + ", " + beneficiary.PinCode.Code;
            }
        }
    }
}