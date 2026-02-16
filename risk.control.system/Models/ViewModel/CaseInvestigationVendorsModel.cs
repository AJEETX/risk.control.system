namespace risk.control.system.Models.ViewModel
{
    public class CaseInvestigationVendorsModel
    {
        public InvestigationTask? CaseTask { get; set; }
        public BeneficiaryDetail? Beneficiary { get; set; }
        public InvestigationReport? InvestigationReport { get; set; }
        public string? Address { get; set; }
        public long? VendorId { get; set; }
        public string? ReportAiSummary { get; set; }
        public bool FromEditPage { get; set; } = false;
        public string? Currency { get; set; }
    }
}