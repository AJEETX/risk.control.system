using System.Globalization;

namespace risk.control.system.Models.ViewModel
{
    public class CaseAgencyAgentModel
    {
        public InvestigationTask ClaimsInvestigation { get; set; } = default!;
        public BeneficiaryDetail Beneficiary { get; set; } = default!;
        public bool ReSelect { get; set; }
        public string Currency { get; set; } = default!;
        public CultureInfo? Culture { get; set; }
    }
}