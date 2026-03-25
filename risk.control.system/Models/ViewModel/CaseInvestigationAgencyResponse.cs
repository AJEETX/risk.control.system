namespace risk.control.system.Models.ViewModel
{
    public class CaseInvestigationAgencyResponse
    {
        public object Id { get; set; } = default!;
        public bool AssignedToAgency { get; set; }
        public string Pincode { get; set; } = default!;
        public string? PincodeName { get; set; }
        public string Document { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Policy { get; set; } = default!;
        public string Customer { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string ServiceType { get; set; } = default!;
        public string? Service { get; set; }
        public string? Location { get; set; }
        public DateTime Created { get; set; } = default!;
        public string timePending { get; set; } = default!;
        public string PolicyNum { get; set; } = default!;
        public string? PolicyId { get; set; }
        public string BeneficiaryPhoto { get; set; } = default!;
        public string BeneficiaryName { get; set; } = default!;
        public string Amount { get; set; } = default!;
        public double TimeElapsed { get; set; }
        public string? Company { get; set; }
        public bool? IsNewAssigned { get; set; }
        public bool? IsQueryCase { get; set; }
        public string OwnerDetail { get; set; } = default!;
        public string PersonMapAddressUrl { get; set; } = default!;
        public string? Distance { get; set; }
        public string? Duration { get; set; }
        public string? AddressLocationInfo { get; set; }
        public bool CanDownload { get; set; } = true;
    }
}