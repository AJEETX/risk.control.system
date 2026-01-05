namespace risk.control.system.Models
{
    public class ClaimsInvestigationResponse
    {
        public object Id { get; set; }
        public bool AssignedToAgency { get; set; }
        public string? Agent { get; set; }
        public string Pincode { get; set; }
        public string? PincodeName { get; set; }
        public string Document { get; set; }
        public string Name { get; set; }
        public string? CustomerFullName { get; set; }
        public string Policy { get; set; }
        public string Customer { get; set; }
        public string Status { get; set; }
        public string SubStatus { get; set; }
        public bool Ready2Assign { get; set; }
        public string ServiceType { get; set; }
        public string? Service { get; set; }
        public string? Location { get; set; }
        public string Created { get; set; }
        public string timePending { get; set; }
        public bool Withdrawable { get; set; }
        public string PolicyNum { get; set; }
        public string? PolicyId { get; set; }
        public string BeneficiaryPhoto { get; set; }
        public string? BeneficiaryFullName { get; set; }
        public string BeneficiaryName { get; set; }
        public string Amount { get; set; }
        public string? Agency { get; set; }
        public double TimeElapsed { get; set; }
        public string? Company { get; set; }
        public bool AutoAllocated { get; set; }
        public bool? IsNewAssigned { get; set; }
        public bool? IsQueryCase { get; set; }
        public string OwnerDetail { get; set; }
        public string AgencyDeclineComment { get; set; }
        public bool CaseWithPerson { get; set; } = false;
        public string? PersonMapAddressUrl { get; set; }
        public string? Distance { get; set; }
        public string? Duration { get; set; }
        public bool CanDownload { get; set; } = true;

    }
}