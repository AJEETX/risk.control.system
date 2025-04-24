namespace risk.control.system.Models.ViewModel
{
    public class CaseTransactionModel
    {
        public InvestigationTask ClaimsInvestigation { get; set; }
        public BeneficiaryDetail Location { get; set; }
        public VendorInvoice? VendorInvoice { get; set; }
        public string? TimeTaken { get; set; }
        public bool Assigned { get; set; } = false;
        public bool AutoAllocation { get; set; } = false;
        public bool Withdrawable { get; set; }
        public bool AllowedToCreate { get; set; } = false ;
        public int? AvailableCount { get; set; } = 0;
        public int? TotalCount { get; set; } = 0;
        public bool Trial { get; set; }
        public string? ReportAiSummary { get; set; }
        public bool CaseIsValidToAssign { get; set; } = false;
    }
}