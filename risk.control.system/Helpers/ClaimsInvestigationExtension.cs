﻿using System;

using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class ClaimsInvestigationExtension
    {
        public static string GetTimePending(this ClaimsInvestigation a)
        {
            if (DateTime.UtcNow.Subtract(a.Created).Days >= 1)
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Days + " day</span>");

            if (DateTime.UtcNow.Subtract(a.Created).Hours < 24 && DateTime.UtcNow.Subtract(a.Created).Hours > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Hours + " hr</span>");
            }
            if (DateTime.UtcNow.Subtract(a.Created).Hours == 0 && DateTime.UtcNow.Subtract(a.Created).Minutes > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Minutes + " min</span>");
            }
            if (DateTime.UtcNow.Subtract(a.Created).Minutes == 0 && DateTime.UtcNow.Subtract(a.Created).Seconds > 0)
            {
                return string.Join("", "<span class='badge badge-light'>" + DateTime.UtcNow.Subtract(a.Created).Seconds + " sec</span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

        public static string GetPolicyNum(this ClaimsInvestigation a)
        {
            var location = a.CaseLocations?.FirstOrDefault(c => c.ClaimsInvestigationId == a.ClaimsInvestigationId);
            if (location is not null)
            {
                var isReview = location.PreviousClaimReports.Count > 0;
                if (isReview)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"REVIEW CASE\"></i>");
                }
            }
            return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style-none\"></i>");
        }

        public static string GetPincode(ClaimType? claimType, CustomerDetail cdetail, CaseLocation location)
        {
            if (claimType == ClaimType.HEALTH)
            {
                if (cdetail is null)
                    return "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>";
                return string.Join("", "<span class='badge badge-light'>" + cdetail.PinCode.Code + "</span>");
            }
            else
            {
                if (location is null)
                    return "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>";
                return string.Join("", "<span class='badge badge-light'>" + location.PinCode.Code + "</span>");
            }
        }
    }
}