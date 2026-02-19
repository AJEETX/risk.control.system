namespace risk.control.system.Models.ViewModel
{
    public class CaseTransactionModel
    {
        public InvestigationTask? ClaimsInvestigation { get; set; }
        public BeneficiaryDetail? Beneficiary { get; set; }
        public bool FromEditPage { get; set; } = false;
        public VendorInvoice? VendorInvoice { get; set; }
        public string? TimeTaken { get; set; }
        public bool Assigned { get; set; } = false;
        public bool Withdrawable { get; set; }
        public bool CanDownload { get; set; }
        public string? ReportAiSummary { get; set; }
        public bool CaseIsValidToAssign { get; set; } = false;
        public string? Currency { get; set; }
    }
}