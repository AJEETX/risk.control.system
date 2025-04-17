namespace risk.control.system.Models.ViewModel
{
    public class InvestigationCreateModel
    {
        public InvestigationTask InvestigationTask { get; set; }
        public BeneficiaryDetail BeneficiaryDetail { get; set; }
        public string? TimeTaken { get; set; }
        public bool Assigned { get; set; } = false;
        public bool AutoAllocation { get; set; } = false;
        public bool NotWithdrawable { get; set; }
        public bool IsQueryCase { get; set; } = false;
        public bool AllowedToCreate { get; set; } = false;
        public int? AvailableCount { get; set; } = 0;
        public int? TotalCount { get; set; } = 0;
        public bool Trial { get; set; }
        public string? ReportAiSummary { get; set; }
    }
}
