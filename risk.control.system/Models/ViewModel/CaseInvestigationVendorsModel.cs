namespace risk.control.system.Models.ViewModel
{
    public class CaseInvestigationVendorsModel
    {
        public QuestionFormViewModel? QuestionFormViewModel { get; set; }
        public InvestigationTask? ClaimsInvestigation { get; set; }
        public BeneficiaryDetail? Location { get; set; }
        public InvestigationReport? InvestigationReport { get; set; }
        public List<VendorCaseModel>? Vendors { get; set; }
        public AssessorRemarkType? AssessorRemarkType { get; set; }
        public string? Address { get; set; }
        public long? VendorId { get; set; }
        public string? ReportAiSummary { get; set; }
        public bool FromEditPage { get; set; } = false;
    }

    public class CaseInvestigationSubmitAnswerModel
    {
        public QuestionFormViewModel? QuestionFormViewModel { get; set; }
        public InvestigationTask? ClaimsInvestigation { get; set; }
        public BeneficiaryDetail? Location { get; set; }
        public InvestigationReport? InvestigationReport { get; set; }

    }

}