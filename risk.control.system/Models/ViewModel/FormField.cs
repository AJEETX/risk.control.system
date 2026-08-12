using risk.control.system.AppConstant;

namespace risk.control.system.Models.ViewModel
{
    public class FormField
    {
        public int Id { get; set; }
        public long CompanyId { get; set; }
        public InsuranceType InsuranceType { get; set; } // Claim or Underwriting
        public string Section { get; set; } = default!;   // "Policy", "Nominee", or "ClaimDetail"
        public string Label { get; set; } = default!;
        public string FieldType { get; set; } = default!;// text, number, date, file, dropdown
        public string? DropdownOptions { get; set; }
        public string? LocationData { get; set; }
        public bool IsRequired { get; set; }
    }
    // Models/SubmittedForm.cs
    public class SubmittedForm : BaseEntity
    {
        public int Id { get; set; }
        public DateTime SubmittedAt { get; set; }
        public long CompanyId { get; set; }
        public InsuranceType InsuranceType { get; set; }
        public List<SubmittedValue> Values { get; set; } = new List<SubmittedValue>();
        public long? VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public bool AssignedToAgency { get; set; } = false;
        public bool Deleted { get; set; } = false;
        public bool IsAutoAllocated { get; set; } = false;
        public bool IsNew { get; set; } = false;
        public bool IsNewAssignedToAgency { get; set; } = false;
        public DateTime? AllocatedToAgencyTime { get; set; } = default!;
        public string? CaseOwner { get; set; } = default!;
        public int? CreatorSla { get; set; } = default!;
        public int? AssessorSla { get; set; } = default!;
        public int? SupervisorSla { get; set; } = default!;
        public int? AgentSla { get; set; } = default!;
        public bool? UpdateAgentAnswer { get; set; } = default!;
        public long? ReportTemplateId { get; set; } = default!;
        public long? InvestigationReportId { get; set; } = default!;
        public InvestigationReport? InvestigationReport { get; set; } = default!;
        public string? WithdrawlComments { get; set; }
        public string Status { get; set; } = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR;
        public ICollection<CaseTimeline> CaseTimelines { get; set; } = new List<CaseTimeline>();

    }

    // Models/SubmittedValue.cs
    public class SubmittedValue
    {
        public int Id { get; set; }
        public int SubmittedFormId { get; set; }
        public int FormFieldId { get; set; }
        public FormField FormField { get; set; } = default!;
        public string Value { get; set; } = default!;// Stores text, dates, or file paths as string
    }

}
