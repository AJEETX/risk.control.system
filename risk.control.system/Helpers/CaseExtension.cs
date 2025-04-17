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
    }
}
