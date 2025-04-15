
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
        public PolicyDetail? PolicyDetail { get; set; }
        public CustomerDetail? CustomerDetail { get; set; }
        public BeneficiaryDetail? BeneficiaryDetail { get; set; }
        public string Status { get; set; }
        public string SubStatus { get; set; }
        public bool IsUploaded { get; set; } = false;
        public bool IsReady2Assign { get; set; } = false;
        public bool AssignedToAgency { get; set; } = false;
        public InvestigationReport InvestigationReport { get; set; }
        public List<CaseNote>? CaseNotes { get; set; } = new();
        public List<CaseMessage>? CaseMessages { get; set; } = new();

        public ORIGIN ORIGIN { get; set; } = ORIGIN.USER;
        public bool AiEnabled { get; set; } = false;
        public bool Deleted { get; set; } = false;
        public DateTime? TaskToAgentTime { get; set; }
        public DateTime? SubmittedToSupervisorTime { get; set; }
        public DateTime? SubmittedToAssessorTime { get; set; }
        public DateTime? ProcessedByAssessorTime { get; set; }
        public DateTime? EnquiredByAssessorTime { get; set; }
        public DateTime? EnquiryReplyByAssessorTime { get; set; }
        public DateTime? ReviewByAssessorTime { get; set; }

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
    }
}
