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
            string title = $"({a.UpdatedBy}) Withdrawn";
            string style = "-none";
            if (a is not null)
            {
                if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY)
                {
                    style = "";
                    title = $"({a.UpdatedBy}) Withdrawn";
                }
                if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY)
                {
                    style = "";
                    title = $"Agency ({a.UpdatedBy}) Declined";
                }
            }
            return string.Join("", a.PolicyDetail?.ContractNumber + $"<i class=\"fa fa-asterisk asterik-style{style}\" title=\"{title}\"></i>");
        }

        public static string GetPolicyNumForAgency(this InvestigationTask a, string id)
        {
            var claim = a;
            if (claim is not null)
            {
                var isRequested = a.SubStatus == id;
                if (isRequested)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"ENQUIRY\"></i>");
                }

            }
            return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style-none\"></i>");
        }

        public static string GetAgentTimePending(this InvestigationTask a, bool open = false)
        {
            DateTime timeToCompare = a.TaskToAgentTime.Value;
            if (open)
            {
                timeToCompare = a.SubmittedToSupervisorTime.Value;
                if (DateTime.Now.Subtract(timeToCompare).Days >= a.AgentSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

                else if (DateTime.Now.Subtract(timeToCompare).Days >= 3 || DateTime.Now.Subtract(timeToCompare).Days >= a.AgentSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");
            }
            else
            {
                if (DateTime.Now.Subtract(timeToCompare).Days >= a.AgentSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.Created).Days} days since created!\"></i>");

                else if (DateTime.Now.Subtract(timeToCompare).Days >= 3 || DateTime.Now.Subtract(timeToCompare).Days >= a.AgentSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(a.Created).Days} day since created.\"></i>");
            }

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
    }
}
