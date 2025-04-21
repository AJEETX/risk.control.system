using risk.control.system.Models;

namespace risk.control.system.Helpers
{
    public static class CaseExtension
    {
        public static bool IsValidPolicy(this PolicyDetail policyDetail)
        {
            return policyDetail != null && policyDetail != null &&
               !string.IsNullOrWhiteSpace(policyDetail.ContractNumber.Trim()) &&
                policyDetail.LineOfBusinessId > 0 &&
                policyDetail.InvestigationServiceTypeId > 0 &&
               !string.IsNullOrWhiteSpace(policyDetail.CauseOfLoss) &&
                policyDetail.SumAssuredValue != null &&
                policyDetail.SumAssuredValue > 0 &&
                policyDetail.ContractIssueDate != null &&
                DateTime.Now > policyDetail.ContractIssueDate &&
                policyDetail.DateOfIncident != null &&
                policyDetail.DateOfIncident > policyDetail.ContractIssueDate &&
                policyDetail.CaseEnablerId > 0 &&
                policyDetail.CostCentreId > 0;
        }

        public static bool IsValidCaseDetail(this PolicyDetail policyDetail)
        {
            return policyDetail != null && policyDetail != null &&
               !string.IsNullOrWhiteSpace(policyDetail.ContractNumber.Trim()) &&
                policyDetail.InsuranceType != null &&
                policyDetail.InvestigationServiceTypeId > 0 &&
               !string.IsNullOrWhiteSpace(policyDetail.CauseOfLoss) &&
                policyDetail.SumAssuredValue != null &&
                policyDetail.SumAssuredValue > 0 &&
                policyDetail.ContractIssueDate != null &&
                DateTime.Now > policyDetail.ContractIssueDate &&
                policyDetail.DateOfIncident != null &&
                policyDetail.DateOfIncident > policyDetail.ContractIssueDate &&
                policyDetail.CaseEnablerId > 0 &&
                policyDetail.CostCentreId > 0;
        }
        public static bool IsValidCaseData(this InvestigationTask claim)
        {
            var validPolicy = claim.PolicyDetail.IsValidCaseDetail();
            var validCustomer = claim.PolicyDetail.IsValidCustomer(claim.CustomerDetail);
            var validBeneficiary = claim.PolicyDetail.IsValidBeneficiary(claim.BeneficiaryDetail);
            return validPolicy && validCustomer && validBeneficiary;
        }
        public static bool IsValidCaseData(this ClaimsInvestigation claim)
        {
            var validPolicy = claim.PolicyDetail.IsValidPolicy();
            var validCustomer = claim.PolicyDetail.IsValidCustomer(claim.CustomerDetail);
            var validBeneficiary = claim.PolicyDetail.IsValidBeneficiary(claim.BeneficiaryDetail);
            return validPolicy && validCustomer && validBeneficiary;
        }
        public static bool IsValidCustomer(this PolicyDetail policyDetail, CustomerDetail customerDetail)
        {
            return customerDetail != null &&
               !string.IsNullOrWhiteSpace(customerDetail.Name) &&
                customerDetail.DateOfBirth != null &&
                policyDetail.ContractIssueDate > (customerDetail.DateOfBirth.GetValueOrDefault()) &&
               !string.IsNullOrWhiteSpace(customerDetail.ContactNumber) &&
                customerDetail.ContactNumber.Length >= 9 &&
                customerDetail.PinCodeId > 0 &&
                customerDetail.DistrictId  > 0 &&
                customerDetail.StateId > 0 &&
                customerDetail.CountryId > 0 &&
                !string.IsNullOrWhiteSpace(customerDetail.Addressline) &&
                customerDetail.ProfilePicture != null;
        }
        public static bool IsValidCustomerForUpload(this PolicyDetail policyDetail, CustomerDetail customerDetail)
        {
            return customerDetail != null &&
               !string.IsNullOrWhiteSpace(customerDetail.Name) &&
                customerDetail.DateOfBirth != null &&
                policyDetail.ContractIssueDate > (customerDetail.DateOfBirth.GetValueOrDefault()) &&
               !string.IsNullOrWhiteSpace(customerDetail.ContactNumber) &&
                customerDetail.ContactNumber.Length >= 9 &&
                customerDetail.PinCodeId > 0 &&
                customerDetail.DistrictId > 0 &&
                customerDetail.StateId > 0 &&
                customerDetail.CountryId > 0 &&
                !string.IsNullOrWhiteSpace(customerDetail.Addressline);
        }
        public static bool IsValidFormCustomer(this PolicyDetail policyDetail, CustomerDetail customerDetail)
        {
            return customerDetail != null &&
               !string.IsNullOrWhiteSpace(customerDetail.Name) &&
                customerDetail.DateOfBirth != null &&
                policyDetail.ContractIssueDate > (customerDetail.DateOfBirth.GetValueOrDefault()) &&
               !string.IsNullOrWhiteSpace(customerDetail.ContactNumber) &&
                customerDetail.ContactNumber.Length >= 9 &&
                customerDetail.SelectedPincodeId > 0 &&
                customerDetail.SelectedDistrictId > 0 &&
                customerDetail.SelectedCountryId > 0 &&
                customerDetail.SelectedStateId > 0 &&
                !string.IsNullOrWhiteSpace(customerDetail.Addressline) &&
                customerDetail.ProfilePicture != null;
        }
        public static bool IsValidBeneficiary(this PolicyDetail policyDetail, BeneficiaryDetail beneficiaryDetail)
        {
            return beneficiaryDetail != null &&
                !string.IsNullOrWhiteSpace(beneficiaryDetail.Name) &&
                beneficiaryDetail.DateOfBirth != null &&
                policyDetail.ContractIssueDate > (beneficiaryDetail.DateOfBirth.GetValueOrDefault()) &&
                !string.IsNullOrWhiteSpace(beneficiaryDetail.ContactNumber) &&
                beneficiaryDetail.ContactNumber.Length >= 9 &&
                beneficiaryDetail.PinCodeId > 0 &&
                beneficiaryDetail.DistrictId > 0 &&
                beneficiaryDetail.StateId > 0 &&
                beneficiaryDetail.CountryId > 0 &&
                !string.IsNullOrWhiteSpace(beneficiaryDetail.Addressline) &&
                beneficiaryDetail.ProfilePicture != null;
        }
        public static bool IsValidBeneficiaryForUpload(this PolicyDetail policyDetail, BeneficiaryDetail beneficiaryDetail)
        {
            return beneficiaryDetail != null &&
                !string.IsNullOrWhiteSpace(beneficiaryDetail.Name) &&
                beneficiaryDetail.DateOfBirth != null &&
                policyDetail.ContractIssueDate > (beneficiaryDetail.DateOfBirth.GetValueOrDefault()) &&
                !string.IsNullOrWhiteSpace(beneficiaryDetail.ContactNumber) &&
                beneficiaryDetail.ContactNumber.Length >= 9 &&
                beneficiaryDetail.PinCodeId > 0 &&
                beneficiaryDetail.DistrictId > 0 &&
                beneficiaryDetail.StateId > 0 &&
                beneficiaryDetail.CountryId > 0 &&
                !string.IsNullOrWhiteSpace(beneficiaryDetail.Addressline);
        }

        public static string GetCreatorTimePending(this InvestigationTask a)
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

        public static string GetAssessorTimePending(this InvestigationTask a, bool assess = false, bool processed = false, bool enquiry = false, bool review = false)
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
            else if (processed)
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

        public static string GetMap(this InvestigationTask claim, bool caseType, bool tasked2Agent = false, bool submitted2Supervisor = false, bool enquiry = false)
        {
            if (tasked2Agent || submitted2Supervisor || enquiry)
            {
                return claim.SelectedAgentDrivingMap;
            }
            return caseType ? claim.CustomerDetail.CustomerLocationMap : claim.BeneficiaryDetail.BeneficiaryLocationMap;
        }
    }
}
