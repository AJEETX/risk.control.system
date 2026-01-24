using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class ClaimsInvestigationExtension
    {
        public static string GetPincode(bool claimType, CustomerDetail cdetail, BeneficiaryDetail location)
        {
            if (claimType)
            {
                if (cdetail is null)
                    return "<span class=\"badge badge-danger\"> <i class=\"fas fa-question\" ></i>  </span>";
                return string.Join("", "<span class='badge badge-light'>" + cdetail.PinCode.Code + "</span>");
            }
            else
            {
                if (location is null)
                    return "<span class=\"badge badge-danger\"> <i class=\"fas fa-question\" ></i>  </span>";
                return string.Join("", "<span class='badge badge-light'>" + location.PinCode.Code + "</span>");
            }
        }

        public static string GetPersonPhoto(bool claimType, CustomerDetail cdetail, BeneficiaryDetail beneficiary)
        {
            if (claimType)
            {
                if (cdetail is not null)
                {
                    return (cdetail.ImagePath);
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

        public static string GetPincodeName(bool claimType, CustomerDetail cdetail, BeneficiaryDetail location)
        {
            if (claimType)
            {
                if (cdetail is null)
                    return "...";
                return cdetail.Addressline + "," + cdetail.District?.Name + ", " + cdetail.State.Name + ", " + cdetail.PinCode.Code;
            }
            else
            {
                if (location is null)
                    return "...";
                return location.Addressline + "," + location.District.Name + ", " + location.State.Name + ", " + location.PinCode.Code;
            }
        }
        public static int GetPincodeCode(bool claimType, CustomerDetail customer, BeneficiaryDetail beneficiary)
        {
            if (claimType)
            {
                if (customer is null)
                    return 0;
                return customer.PinCode.Code;
            }
            else
            {
                if (beneficiary is null)
                    return 0;
                return beneficiary.PinCode.Code;
            }
        }

    }
}