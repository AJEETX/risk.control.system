namespace risk.control.system.Models.ViewModel
{
    public class ClaimTransactionModel
    {
        public ClaimsInvestigation ClaimsInvestigation { get; set; }
        public CaseLocation Location { get; set; }
        public VendorInvoice? VendorInvoice { get; set; }
        public List<InvestigationTransaction> Log { get; set; }
    }
}