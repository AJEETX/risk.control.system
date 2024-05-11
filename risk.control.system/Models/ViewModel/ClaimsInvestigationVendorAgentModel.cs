namespace risk.control.system.Models.ViewModel
{
    public class ClaimsInvestigationVendorAgentModel
    {
        public ClaimsInvestigation ClaimsInvestigation { get; set; }
        public BeneficiaryDetail CaseLocation { get; set; }
        public List<VendorUserClaim> VendorUserClaims { get; set; }
    }

    public class VendorUserClaim
    {
        public VendorApplicationUser AgencyUser { get; set; }
        public int CurrentCaseCount { get; set; }
    }
}