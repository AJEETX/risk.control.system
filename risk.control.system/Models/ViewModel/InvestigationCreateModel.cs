namespace risk.control.system.Models.ViewModel
{
    public class InvestigationCreateModel
    {
        public InvestigationTask InvestigationTask { get; set; }
        public BeneficiaryDetail BeneficiaryDetail { get; set; }
        public bool AutoAllocation { get; set; } = false;
        public bool AllowedToCreate { get; set; } = false;
        public int? AvailableCount { get; set; } = 0;
        public int? TotalCount { get; set; } = 0;
        public bool Trial { get; set; }
    }
}
