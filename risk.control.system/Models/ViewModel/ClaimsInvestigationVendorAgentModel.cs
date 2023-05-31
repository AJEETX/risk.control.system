namespace risk.control.system.Models.ViewModel
{
    public class ClaimsInvestigationVendorAgentModel
    {
        public ClaimsInvestigation ClaimsInvestigation { get; set; }
        public CaseLocation CaseLocation { get; set; }
        public List<VendorApplicationUser> VendorApplicationUser { get; set; }
    }
}
