namespace risk.control.system.Models.ViewModel
{
    public class ClaimTransactionModel
    {
        public ClaimsInvestigation ClaimsInvestigation { get; set; }
        public CaseLocation Location { get; set; }
        public VendorInvoice? VendorInvoice { get; set; }
        public List<InvestigationTransaction> Log { get; set; }
        public string? TimeTaken { get; set; }
        public bool Assigned { get; set; } = false;
        public bool AutoAllocation { get; set; } = false;
        public bool NotWithdrawable { get; set; }
        public bool AllowedToCreate { get; set; } = false ;
    }
}