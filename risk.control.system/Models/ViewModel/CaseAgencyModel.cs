namespace risk.control.system.Models.ViewModel
{
    public class CaseAgencyModel
    {
        public InvestigationTask? CaseTask { get; set; }
        public BeneficiaryDetail? Beneficiary { get; set; }
        public InvestigationReport? InvestigationReport { get; set; }
        public string? Address { get; set; }
        public string? ReportAiSummary { get; set; }
        public string? Currency { get; set; }
    }
}