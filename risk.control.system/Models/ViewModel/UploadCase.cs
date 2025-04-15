namespace risk.control.system.Models.ViewModel
{
    public class UploadCase
    {
        public string CaseType { get; set; } = "0";
        public string CaseId { get; set; }
        public string? ServiceType { get; set; }
        public string? Reason { get; set; }
        public string Amount { get; set; }
        public string IssueDate { get; set; }
        public string IncidentDate { get; set; }
        public string? Cause { get; set; }
        public string? Department { get; set; }
        public string CustomerName { get; set; }
        public string? CustomerType { get; set; }
        public string? Gender { get; set; }
        public string CustomerDob { get; set; }
        public string CustomerContact { get; set; }
        public string? Education { get; set; }
        public string? Occupation { get; set; }
        public string? Income { get; set; }
        public string CustomerAddressLine { get; set; }
        public string CustomerPincode { get; set; }
        public string BeneficiaryName { get; set; }
        public string? Relation { get; set; }
        public string BeneficiaryDob { get; set; }
        public string? BeneficiaryIncome { get; set; }
        public string BeneficiaryContact { get; set; }
        public string BeneficiaryAddressLine { get; set; }
        public string BeneficiaryPincode { get; set; }
    }
}
