namespace risk.control.system.Models.ViewModel
{
    public class CaseInvestigationVendorAgentModel
    {
        public InvestigationTask ClaimsInvestigation { get; set; }
        public BeneficiaryDetail CaseLocation { get; set; }
        public bool ReSelect { get; set; }
        public string Currency { get; set; }
    }
}