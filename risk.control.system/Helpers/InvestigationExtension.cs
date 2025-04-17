using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class InvestigationExtension
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
        public static string GetPolicyNum(this InvestigationTask a)
        {
            string title = "Withdrawn by company";
            string style = "none";
            if (a is not null)
            {
                if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY)
                {
                    style = "";
                    title = "Withdrawn by company";
                }
                if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY)
                {
                    style = "";
                    title = "Withdrawn by agency";
                }
            }
            return string.Join("", a.PolicyDetail?.ContractNumber + $"<i class=\"fa fa-asterisk asterik-style-{style}\" title=\"{title}\"></i>");
        }
    }
}
