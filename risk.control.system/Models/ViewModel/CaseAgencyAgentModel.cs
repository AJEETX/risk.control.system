using System.Globalization;

namespace risk.control.system.Models.ViewModel
{
    public class CaseAgencyAgentModel
    {
        public InvestigationTask ClaimsInvestigation { get; set; } = default!;
        public BeneficiaryDetail Beneficiary { get; set; } = default!;
        public bool ReSelect { get; set; } = false;
        public string Currency { get; set; } = default!;
        public CultureInfo? Culture { get; set; }
        public bool Withdrawable { get; set; }
        public bool Route { get; set; } = true;
    }
}