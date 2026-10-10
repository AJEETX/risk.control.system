namespace risk.control.system.Models.ViewModel
{
    public class AdjudicationRequest
    {
        public string PolicyNumber { get; set; } = default!;
        public string Email { get; set; }
        public bool Set { get; set; } = true;
    }

    public class SubmitCaseRequest
    {
        public string AssessorRemarks { get; set; } = default!;
        public string AssessorRemarkType { get; set; } = default!;
        public long ClaimId { get; set; }
        public string Email { get; set; } = default!;
    }
}
