using System;

using Highsoft.Web.Mvc.Charts;

using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class ClaimsInvestigationExtension
    {
        public static string GetCreatorTimePending(this ClaimsInvestigation a, bool active = false, bool review = false)
        {
            if (!active)
            {
                if(a.CreatorSla == 0)
                {
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Created).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.Created).Days} days since created!\"></i>");
                }
                if(review)
                {
                    if (DateTime.Now.Subtract(a.ReviewByAssessorTime.Value).Days >= a.CreatorSla)
                        return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ReviewByAssessorTime.Value).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.Created).Days} days since created!\"></i>");

                    else if (DateTime.Now.Subtract(a.ReviewByAssessorTime.Value).Days >= 3 || DateTime.Now.Subtract(a.ReviewByAssessorTime.Value).Days >= a.CreatorSla)
                        return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ReviewByAssessorTime.Value).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(a.Created).Days} day since created.\"></i>");
                    if (DateTime.Now.Subtract(a.ReviewByAssessorTime.Value).Days >= 1)
                        return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.ReviewByAssessorTime.Value).Days} day</span>");
                    if (DateTime.Now.Subtract(a.Created).Hours < 24 &&
                    DateTime.Now.Subtract(a.Created).Hours > 0)
                    {
                        return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Created).Hours} hr </span>");
                    }
                    if (DateTime.Now.Subtract(a.Created).Hours == 0 && DateTime.Now.Subtract(a.Created).Minutes > 0)
                    {
                        return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Created).Minutes} min </span>");
                    }
                    if (DateTime.Now.Subtract(a.Created).Minutes == 0 && DateTime.Now.Subtract(a.Created).Seconds > 0)
                    {
                        return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Created).Seconds} sec </span>");
                    }
                    return string.Join("", "<span class='badge badge-light'>now</span>");
                }

                if (DateTime.Now.Subtract(a.Created).Days >= a.CreatorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Created).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.Created).Days} days since created!\"></i>");

                else if (DateTime.Now.Subtract(a.Created).Days >= 3 || DateTime.Now.Subtract(a.Created).Days >= a.CreatorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Created).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(a.Created).Days} day since created.\"></i>");
                if (DateTime.Now.Subtract(a.Created).Days >= 1)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Created).Days} day</span>");

                if (DateTime.Now.Subtract(a.Created).Hours < 24 &&
                    DateTime.Now.Subtract(a.Created).Hours > 0)
                {
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Created).Hours} hr </span>");
                }
                if (DateTime.Now.Subtract(a.Created).Hours == 0 && DateTime.Now.Subtract(a.Created).Minutes > 0)
                {
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Created).Minutes} min </span>");
                }
                if (DateTime.Now.Subtract(a.Created).Minutes == 0 && DateTime.Now.Subtract(a.Created).Seconds > 0)
                {
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Created).Seconds} sec </span>");
                }
                return string.Join("", "<span class='badge badge-light'>now</span>");
            }
            else
            {
                if (a.CreatorSla == 0)
                {
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} days since created!\"></i>");
                }
                if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= a.CreatorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");

                else if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= 3 || DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= a.CreatorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");
                if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days >= 1)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Days} day</span>");

                if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours < 24 &&
                    DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours > 0)
                {
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours} hr </span>");
                }
                if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Hours == 0 && DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Minutes > 0)
                {
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Minutes} min </span>");
                }
                if (DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Minutes == 0 && DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Seconds > 0)
                {
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).Seconds} sec </span>");
                }
                return string.Join("", "<span class='badge badge-light'>now</span>");
            }
            
        }

        public static string GetAgentTimePending(this ClaimsInvestigation a, bool open = false)
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

        public static string GetSupervisorTimePending(this ClaimsInvestigation a, bool newClaim = false, bool taskedToAgent = false, bool submitted = false, bool completed = false, bool enquiry = false)
        {
            DateTime timeToCompare = a.AllocatedToAgencyTime.Value;
            if(newClaim)
            {
                timeToCompare = a.AllocatedToAgencyTime.Value;
                if (DateTime.Now.Subtract(timeToCompare).Days >= a.SupervisorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(timeToCompare).Days} days since created!\"></i>");

                else  if (DateTime.Now.Subtract(timeToCompare).Days >= 3 || DateTime.Now.Subtract(timeToCompare).Days >= a.SupervisorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(timeToCompare).Days} day since created.\"></i>");
            }
            else if(taskedToAgent)
            {
                timeToCompare = a.TaskToAgentTime.Value;
                if (DateTime.Now.Subtract(timeToCompare).Days >= a.SupervisorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

                else if (DateTime.Now.Subtract(timeToCompare).Days >= 3 || DateTime.Now.Subtract(timeToCompare).Days >= a.SupervisorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            }
            else if(submitted)
            {
                timeToCompare = a.SubmittedToSupervisorTime.Value;
                if (DateTime.Now.Subtract(timeToCompare).Days >= a.SupervisorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(timeToCompare).Days} days since created!\"></i>");

                else if (DateTime.Now.Subtract(timeToCompare).Days >= 3 || DateTime.Now.Subtract(timeToCompare).Days >= a.SupervisorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(timeToCompare).Days} day since created.\"></i>");

            }
            else if(completed)
            {
                timeToCompare = a.SubmittedToAssessorTime.Value;
                if (DateTime.Now.Subtract(timeToCompare).Days >= a.SupervisorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

                else if (DateTime.Now.Subtract(timeToCompare).Days >= 3 || DateTime.Now.Subtract(timeToCompare).Days >= a.SupervisorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");
            }

            else if (enquiry)
            {
                timeToCompare = a.EnquiredByAssessorTime.Value;
                if (DateTime.Now.Subtract(timeToCompare).Days >= a.SupervisorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

                else if (DateTime.Now.Subtract(timeToCompare).Days >= 3 || DateTime.Now.Subtract(timeToCompare).Days >= a.SupervisorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");
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
        public static string GetAssessorTimePending(this ClaimsInvestigation a, bool assess = false,bool processed= false, bool enquiry = false, bool review = false)
        {
            DateTime time2Compare = a.SubmittedToAssessorTime.Value;
            if (assess)
            {
                time2Compare = a.SubmittedToAssessorTime.Value;
                if (DateTime.Now.Subtract(time2Compare).Days >= a.AssessorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(time2Compare).Days} days since created!\"></i>");

                else if (DateTime.Now.Subtract(time2Compare).Days >= 3 || DateTime.Now.Subtract(time2Compare).Days >= a.AssessorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(time2Compare).Days} day since created.\"></i>");

            }
            else if(processed)
            {
                time2Compare = a.ProcessedByAssessorTime.Value;
                if (DateTime.Now.Subtract(time2Compare).Days >= a.AssessorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Days} day</span>");

                else if (DateTime.Now.Subtract(time2Compare).Days >= 3 || DateTime.Now.Subtract(time2Compare).Days >= a.AssessorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Days} day</span>");

            }
            else if (enquiry)
            {
                time2Compare = a.EnquiredByAssessorTime.Value;
                if (DateTime.Now.Subtract(time2Compare).Days >= a.AssessorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Days} day</span>");

                else if (DateTime.Now.Subtract(time2Compare).Days >= 3 || DateTime.Now.Subtract(time2Compare).Days >= a.AssessorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Days} day</span>");
            }
            else if (review)
            {
                time2Compare = a.ReviewByAssessorTime.Value;
                if (DateTime.Now.Subtract(time2Compare).Days >= a.AssessorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Days} day</span>");

                else if (DateTime.Now.Subtract(time2Compare).Days >= 3 || DateTime.Now.Subtract(time2Compare).Days >= a.AssessorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Days} day</span>");
            }

            if (DateTime.Now.Subtract(time2Compare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Days} day</span>");

            if (DateTime.Now.Subtract(time2Compare).Hours < 24 &&
                DateTime.Now.Subtract(time2Compare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(time2Compare).Hours == 0 && DateTime.Now.Subtract(time2Compare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(time2Compare).Minutes == 0 && DateTime.Now.Subtract(time2Compare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(time2Compare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        public static string GetManagerTimePending(this ClaimsInvestigation a, bool assess= false, bool completed = false)
        {
            DateTime timeToCompare = a.Created;
            if (assess)
            {
                timeToCompare = a.SubmittedToAssessorTime.Value;
                if (DateTime.Now.Subtract(timeToCompare).Days >= a.AssessorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(timeToCompare).Days} days since created!\"></i>");
            }
            else if(completed)
            {
                timeToCompare = a.ProcessedByAssessorTime.Value;
                if (DateTime.Now.Subtract(timeToCompare).Days >= a.AssessorSla)
                    return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(timeToCompare).Days} days since created!\"></i>");
            }

            if (DateTime.Now.Subtract(timeToCompare).Days >= 3 || DateTime.Now.Subtract(timeToCompare).Days >= a.AssessorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(a.Created).Days} day since created.\"></i>");

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{ DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{ DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(a.Created).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

        public static string GetMapUrl(this ClaimsInvestigation claim,bool tasked2Agent = false, bool submitted2Supervisor = false, bool enquiry = false)
        {
            if(tasked2Agent || submitted2Supervisor || enquiry)
            {
                return claim.SelectedAgentDrivingMap;
            }
            return claim.PolicyDetail.ClaimType == ClaimType.HEALTH ? claim.CustomerDetail.CustomerLocationMap : claim.BeneficiaryDetail.BeneficiaryLocationMap;
        }
        public static string GetPolicyNum(this ClaimsInvestigation a)
        {
            var claim = a;
            if (claim is not null)
            {
                var isReview = a.PreviousClaimReports.Count > 0;
                var isEnquiry = a.IsQueryCase;
                var status = a.InvestigationCaseSubStatus.Name.ToUpper();
                if (status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + $"<i class=\"fa fa-asterisk asterik-style\" title=\"{a.CompanyWithdrawlComment}\"></i>");
                }
                if (status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + $"<i class=\"fa fa-asterisk asterik-style\" title=\"{a.AgencyDeclineComment}\"></i>");
                }
                if (isReview && isEnquiry) {
                    return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"REVIEW CASE\"></i><i class=\"fa fa-asterisk asterik-style\" title=\"ENQUIRY REPLY\"></i>");
                }
                if (isReview)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"REVIEW CASE\"></i>");
                }
                if(isEnquiry)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"ENQUIRY REPLY\"></i>");
                }
            }
            return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style-none\"></i>");
        }
        public static string GetPolicyNumForAgency(this ClaimsInvestigation a, string id)
        {
            var claim = a;
            if (claim is not null)
            {
                var isRequested = a.InvestigationCaseSubStatusId == id;
                if (isRequested)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"ENQUIRY\"></i>");
                }
            }
            return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style-none\"></i>");
        }

        public static string GetPincode(ClaimType? claimType, CustomerDetail cdetail, BeneficiaryDetail location)
        {
            if (claimType == ClaimType.HEALTH)
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

        public static string GetPersonPhoto(ClaimType? claimType, CustomerDetail cdetail, BeneficiaryDetail beneficiary)
        {
            if (claimType == ClaimType.HEALTH)
            {
                if (cdetail is not null)
                {
                    return string.Format("data:image/*;base64,{0}", Convert.ToBase64String(cdetail.ProfilePicture));
                }
            }
            if (claimType == ClaimType.DEATH)
            {
                if(beneficiary is not null)
                {
                    return string.Format("data:image/*;base64,{0}", Convert.ToBase64String(beneficiary.ProfilePicture));
                }
            }
            return Applicationsettings.NO_USER;
        }
        public static string GetPincodeName(ClaimType? claimType, CustomerDetail cdetail, BeneficiaryDetail location)
        {
            if (claimType == ClaimType.HEALTH)
            {
                if (cdetail is null)
                    return "...";
                return cdetail.Addressline + "," + cdetail.District.Name + ", " + cdetail.State.Name + ", " + cdetail.PinCode.Code;
            }
            else
            {
                if (location is null)
                    return "...";
                return location.Addressline + "," + location.District.Name + ", " + location.State.Name + ", " + location.PinCode.Code;
            }
        }

        public static string GetElapsedTime(this List<InvestigationTransaction> caseLogs)
        {
            var orderedLogs = caseLogs.OrderBy(l => l.Created);

            var startTime = orderedLogs.FirstOrDefault();
            var completedTime = orderedLogs.LastOrDefault();
            var elaspedTime = completedTime.Created.Subtract(startTime.Created).Days;
            if (completedTime.Created.Subtract(startTime.Created).Days >= 1)
            {
                return elaspedTime + " day(s)";
            }
            if (completedTime.Created.Subtract(startTime.Created).TotalHours < 24 && completedTime.Created.Subtract(startTime.Created).TotalHours >= 1)
            {
                return completedTime.Created.Subtract(startTime.Created).Hours + " hour(s)";
            }
            if (completedTime.Created.Subtract(startTime.Created).Minutes < 60 && completedTime.Created.Subtract(startTime.Created).Minutes >= 1)
            {
                return completedTime.Created.Subtract(startTime.Created).Minutes + " min(s)";
            }
            return completedTime.Created.Subtract(startTime.Created).Seconds + " sec";
        }
    }
}