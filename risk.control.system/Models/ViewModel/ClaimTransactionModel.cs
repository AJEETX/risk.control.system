namespace risk.control.system.Models.ViewModel
{
    public class ClaimTransactionModel
    {
        public ClaimsInvestigation Claim { get; set; }
        public List<InvestigationTransaction> Log { get; set; }
    }
}