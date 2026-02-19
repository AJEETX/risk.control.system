namespace risk.control.system.Models.ViewModel
{
    public class CaseAgencyAgentModel
    {
        public InvestigationTask ClaimsInvestigation { get; set; }
        public BeneficiaryDetail Beneficiary { get; set; }
        public bool ReSelect { get; set; }
        public string Currency { get; set; }
    }
}