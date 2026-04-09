using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class InvestigationExtension
    {
        public static string GetPolicyNum(this InvestigationTask caseTask, string contractNumber = "")
        {
            string title = $"";
            string style = "-none";
            if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY)
            {
                style = "";
                title = $"({caseTask.UpdatedBy}) {CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY}";
                return $"<sup><i class=\"fa fa-asterisk asterik-style{style}\" title=\"{title}\"></i></sup>" + contractNumber;
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY)
            {
                style = "";
                title = $"Agency ({caseTask.UpdatedBy}) {CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY}";
                return $"<sup><i class=\"fa fa-asterisk asterik-style{style}\" title=\"{title}\"></i></sup>" + contractNumber;
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                style = "";
                title = $"Agency ({caseTask.UpdatedBy}) {CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR}";
                return $"<sup><i class=\"fa fa-asterisk asterik-style{style}\" title=\"{title}\"></i></sup>" + contractNumber;
            }
            else if (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)
            {
                style = "";
                title = $"Company ({caseTask.UpdatedBy}) {CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR}";
                return $"<sup><i class=\"fa fa-asterisk asterik-style{style}\" title=\"{title}\"></i></sup>" + contractNumber;
            }
            return contractNumber;
        }

        public static string GetPolicyNumForAgency(this InvestigationTask caseTask, string id)
        {
            var claim = caseTask;
            if (claim is not null)
            {
                var isRequested = caseTask.SubStatus == id;
                if (isRequested)
                {
                    return string.Join("", "<i class=\"fa fa-asterisk asterik-style\" title=\"ENQUIRY\"></i>" + caseTask.PolicyDetail?.ContractNumber);
                }
            }
            return string.Join("", "<i class=\"fa fa-asterisk asterik-style-none\"></i>" + caseTask.PolicyDetail?.ContractNumber);
        }

        public static string GetAgentTimePending(this InvestigationTask caseTask, bool open = false)
        {
            // 1. Determine base time and calculate duration once
            DateTime baseTime = open ? caseTask.SubmittedToSupervisorTime!.Value : caseTask.TaskToAgentTime!.Value;
            TimeSpan elapsed = DateTime.UtcNow.Subtract(baseTime);

            // 2. Handle the "SLA/Warning" logic (The logic-heavy part)
            if (elapsed.Days >= caseTask.AgentSla)
            {
                return BuildSlaBadge(elapsed.Days, caseTask.AgentSla, open);
            }

            // 3. Handle the "Standard Time Display" logic
            return BuildStandardTimeBadge(elapsed);
        }

        private static string BuildSlaBadge(int elapsedDays, int sla, bool open)
        {
            string badge = $"<span class='badge badge-light'>{elapsedDays} day</span>";

            // If it's "open", we just return the badge. 
            // Otherwise, append the specific warning icon.
            if (!open)
            {
                string title = elapsedDays >= sla
                    ? $"Hurry up, {elapsedDays} days since allocated!"
                    : $"Caution : {elapsedDays} day since allocated.";

                return $"{badge}<i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"{title}\"></i>";
            }

            return badge;
        }

        private static string BuildStandardTimeBadge(TimeSpan elapsed)
        {
            string value;
            if (elapsed.Days >= 1) value = $"{elapsed.Days} day";
            else if (elapsed.Hours > 0) value = $"{elapsed.Hours} hr";
            else if (elapsed.Minutes > 0) value = $"{elapsed.Minutes} min";
            else if (elapsed.Seconds > 0) value = $"{elapsed.Seconds} sec";
            else value = "now";

            return $"<span class='badge badge-light'>{value}</span>";
        }
    }
}