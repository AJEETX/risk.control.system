namespace risk.control.system.Models.ViewModel
{
    public class CaseInvestigationResponse
    {
        public object Id { get; set; } = default!;
        public bool AssignedToAgency { get; set; }
        public string? Agent { get; set; }
        public string Pincode { get; set; } = default!;
        public string? PincodeName { get; set; }
        public string Document { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? CustomerFullName { get; set; }
        public string Policy { get; set; } = default!;
        public string Customer { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string SubStatus { get; set; } = default!;
        public bool Ready2Assign { get; set; }
        public string ServiceType { get; set; } = default!;
        public string? Service { get; set; }
        public string? Location { get; set; }
        public DateTime Created { get; set; } = default!;
        public string timePending { get; set; } = default!;
        public bool Withdrawable { get; set; }
        public string PolicyNum { get; set; } = default!;
        public string? PolicyId { get; set; }
        public string BeneficiaryPhoto { get; set; } = default!;
        public string? BeneficiaryFullName { get; set; }
        public string BeneficiaryName { get; set; } = default!;
        public string Amount { get; set; } = default!;
        public string? Agency { get; set; }
        public double TimeElapsed { get; set; }
        public string? Company { get; set; }
        public bool AutoAllocated { get; set; }
        public string OwnerDetail { get; set; } = default!;
        public bool CaseWithPerson { get; set; } = false;
        public string? PersonMapAddressUrl { get; set; }
        public string? Distance { get; set; }
        public string? Duration { get; set; }
        public bool CanDownload { get; set; } = true;
        public bool IsNewSubmittedToCompany { get; set; }
    }
}