
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace risk.control.system.Models
{
    public class InvestigationTask : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long? ClientCompanyId { get; set; }
        public ClientCompany? ClientCompany { get; set; }
        public long? VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public long? InvestigationReportId { get; set; }
        public InvestigationReport? InvestigationReport { get; set; }
        public long? ReportTemplateId { get; set; }
        public ReportTemplate? ReportTemplate { get; set; }

        public PolicyDetail? PolicyDetail { get; set; }
        public CustomerDetail? CustomerDetail { get; set; }
        public BeneficiaryDetail? BeneficiaryDetail { get; set; }
        public string Status { get; set; }
        public string SubStatus { get; set; }
        public string? CaseOwner { get; set; }
        public bool IsUploaded { get; set; } = false;
        public bool IsReady2Assign { get; set; } = false;
        public bool IsAutoAllocated { get; set; } = false;
        public bool AssignedToAgency { get; set; } = false;

        public List<CaseNote>? CaseNotes { get; set; } = new();
        public List<CaseMessage>? CaseMessages { get; set; } = new();

        public ORIGIN ORIGIN { get; set; } = ORIGIN.USER;
        public bool AiEnabled { get; set; } = false;
        public bool Deleted { get; set; } = false;
        public bool IsQueryCase { get; set; } = false;
        public string? AllocatingSupervisordEmail { get; set; }
        public string? SubmittingSupervisordEmail { get; set; }
        public string? SubmittedAssessordEmail { get; set; }
        public string? RequestedAssessordEmail { get; set; }
        public string? TaskedAgentEmail { get; set; }
        public DateTime? TaskToAgentTime { get; set; }
        public DateTime? SubmittedToSupervisorTime { get; set; }
        public DateTime? SubmittedToAssessorTime { get; set; }
        public DateTime? ProcessedByAssessorTime { get; set; }
        public DateTime? EnquiredByAssessorTime { get; set; }
        public DateTime? EnquiryReplyByAssessorTime { get; set; }
        public DateTime? ReviewByAssessorTime { get; set; }
        public DateTime? AllocatedToAgencyTime { get; set; }
        public bool IsNew { get; set; } = true;
        public bool IsNewAssignedToManager { get; set; } = true;
        public bool IsNewAssignedToAgency { get; set; } = true;
        public bool IsNewSubmittedToAgent { get; set; } = true;
        public bool IsNewSubmittedToAgency { get; set; } = true;
        public bool IsNewSubmittedToCompany { get; set; } = true;
        public bool IsNewProcessedByCompany { get; set; } = true;
        public bool IsNewReviewedByCompany { get; set; } = true;
        public int CreatorSla { get; set; } = 5;
        public int AssessorSla { get; set; } = 5;
        public int SupervisorSla { get; set; } = 5;
        public int AgentSla { get; set; } = 5;
        public bool UpdateAgentAnswer { get; set; } = false;
        public string? SelectedAgentDrivingMap { get; set; }
        [Display(Name = "Distance")]
        public string? SelectedAgentDrivingDistance { get; set; } = default!;
        public float? SelectedAgentDrivingDistanceInMetres { get; set; } = default!;

        [Display(Name = "Duration")]
        public string? SelectedAgentDrivingDuration { get; set; } = default!;
        public int? SelectedAgentDrivingDurationInSeconds { get; set; } = default!;
        public ICollection<InvestigationTimeline> InvestigationTimeline { get; set; }
    }

    public enum CASETYPE
    {
        CLAIM,
        UNDERWRITING
    }
}
