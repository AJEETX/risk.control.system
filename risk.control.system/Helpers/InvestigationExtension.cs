using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class InvestigationExtension
    {

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
        public static string GetPolicyNum(this InvestigationTask caseTask)
        {
            string title = $"";
            string style = "-none";
            if (caseTask is not null)
            {
                if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY)
                {
                    style = "";
                    title = $"({caseTask.UpdatedBy}) Withdrawn";
                }
                else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY)
                {
                    style = "";
                    title = $"Agency ({caseTask.UpdatedBy}) Declined";
                }
                else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
                {
                    style = "";
                    title = $"Agency ({caseTask.UpdatedBy}) Reply";
                }
            }
            return string.Join("", caseTask.PolicyDetail?.ContractNumber + $"<i class=\"fa fa-asterisk asterik-style{style}\" title=\"{title}\"></i>");
        }

        public static string GetPolicyNumForAgency(this InvestigationTask caseTask, string id)
        {
            var claim = caseTask;
            if (claim is not null)
            {
                var isRequested = caseTask.SubStatus == id;
                if (isRequested)
                {
                    return string.Join("", caseTask.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"ENQUIRY\"></i>");
                }

            }
            return string.Join("", caseTask.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style-none\"></i>");
        }

        public static string GetAgentTimePending(this InvestigationTask caseTask, bool open = false)
        {
            DateTime timeToCompare = caseTask.TaskToAgentTime.Value;
            if (open)
            {
                timeToCompare = caseTask.SubmittedToSupervisorTime.Value;
                if (DateTime.Now.Subtract(timeToCompare).Days >= caseTask.AgentSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

                else if (DateTime.Now.Subtract(timeToCompare).Days >= 3 || DateTime.Now.Subtract(timeToCompare).Days >= caseTask.AgentSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");
            }
            else
            {
                if (DateTime.Now.Subtract(timeToCompare).Days >= caseTask.AgentSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(caseTask.Created).Days} days since created!\"></i>");

                else if (DateTime.Now.Subtract(timeToCompare).Days >= 3 || DateTime.Now.Subtract(timeToCompare).Days >= caseTask.AgentSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(caseTask.Created).Days} day since created.\"></i>");
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
