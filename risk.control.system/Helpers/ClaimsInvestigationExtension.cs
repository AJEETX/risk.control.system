﻿using System;

using Highsoft.Web.Mvc.Charts;

using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class ClaimsInvestigationExtension
    {
        public static string GetTimePending(this ClaimsInvestigation a)
        {
            if (DateTime.Now.Subtract(a.Created).Days >= 1)
                return string.Join("", "<span class='badge badge-light'>" + DateTime.Now.Subtract(a.Created).Days + " day</span>");

            if (DateTime.Now.Subtract(a.Created).Hours < 24 &&
                DateTime.Now.Subtract(a.Created).Hours > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.Now.Subtract(a.Created).Hours + " hr</span>");
            }
            if (DateTime.Now.Subtract(a.Created).Hours == 0 && DateTime.Now.Subtract(a.Created).Minutes > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.Now.Subtract(a.Created).Minutes + " min</span>");
            }
            if (DateTime.Now.Subtract(a.Created).Minutes == 0 && DateTime.Now.Subtract(a.Created).Seconds > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.Now.Subtract(a.Created).Seconds + " sec</span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

        public static string GetPolicyNum(this ClaimsInvestigation a)
        {
            var location = a;
            if (location is not null)
            {
                var isReview = a.PreviousClaimReports.Count > 0;
                if (isReview)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"REVIEW CASE\"></i>");
                }
                var isEnquiry = a.IsQueryCase;
                if(isEnquiry)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"ENQUIRY REPLY\"></i>");
                }
            }
            return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style-none\"></i>");
        }
        public static string GetPolicyNumForAgency(this ClaimsInvestigation a, string id)
        {
            var location = a;
            if (location is not null)
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