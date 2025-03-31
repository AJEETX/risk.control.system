using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using risk.control.system.Helpers;

namespace risk.control.system.Models
{
    public class ClaimsInvestigation : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ClaimsInvestigationId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Display(Name = "Insurer")]
        public long? ClientCompanyId { get; set; }

        [Display(Name = "Insurer")]
        public ClientCompany? ClientCompany { get; set; }

        [Display(Name = "Agency name")]
        public long? VendorId { get; set; }

        [Display(Name = "Agency name")]
        public Vendor? Vendor { get; set; }

        public long? PolicyDetailId { get; set; }
        public PolicyDetail? PolicyDetail { get; set; }
        public CustomerDetail? CustomerDetail { get; set; }
        public BeneficiaryDetail? BeneficiaryDetail { get; set; }

        [NotMapped]
        public bool HasClientCompany { get; set; } = true;

        public List<Vendor>? Vendors { get; set; } = new();

        [Display(Name = "Case status")]
        public string? InvestigationCaseStatusId { get; set; } = default!;

        [Display(Name = "Case status")]
        public InvestigationCaseStatus? InvestigationCaseStatus { get; set; } = default!;

        [Display(Name = "Case sub status")]
        public string? InvestigationCaseSubStatusId { get; set; } = default!;

        [Display(Name = "Case sub status")]
        public InvestigationCaseSubStatus? InvestigationCaseSubStatus { get; set; } = default!;

        public bool AssignedToAgency { get; set; } = false;

        public string? CurrentUserEmail { get; set; }
        public AgencyReport? AgencyReport { get; set; } = new();

        public List<PreviousClaimReport> PreviousClaimReports { get; set; } = new List<PreviousClaimReport>();

        public List<ClaimNote>? ClaimNotes { get; set; } = new();
        public string? CurrentClaimOwner { get; set; }
        public List<ClaimMessage>? ClaimMessages { get; set; } = new();
        public List<SmsNotification>? SmsNotifications { get; set; } = new();
        public List<StatusNotification>? Notifications { get; set; } = new();
        public CREATEDBY CREATEDBY { get; set; } = CREATEDBY.MANUAL;
        public ORIGIN ORIGIN { get; set; } = ORIGIN.USER;

        public bool AiEnabled { get; set; } = false;
        public override string ToString()
        {
            return $"Insurance Claim Investigation Information:\n" +
            $"- Investigation Id: {ClaimsInvestigationId}\n" +
            $"- Policy Detail: {PolicyDetail.ToString()}\n" +
            $"- Customer: {CustomerDetail.ToString()} \n" +
            $"- Beneficiary: {BeneficiaryDetail.ToString()} \n" +
            $"- Agency report: {AgencyReport.ToString()} \n" +
            $"- Case creation type: {CREATEDBY.GetEnumDisplayName()} \n" +
            $"- Case created by: {ORIGIN.GetEnumDisplayName()}\n" +
            $"- Case Status: {InvestigationCaseStatus} \n" +
            $"- Case SubStatus: {InvestigationCaseSubStatus}";
        }

        public bool EnablePassport { get; set; } = false;
        public bool EnableMedia { get; set; } = false;
        public bool IsReviewCase { get; set; } = false;
        public bool NotWithdrawable { get; set; } = false;
        public bool NotDeclinable { get; set; } = false;
        public bool IsQueryCase { get; set; } = false;
        public bool IsReady2Assign { get; set; } = false;
        public bool AutoAllocated { get; set; } = true;
        public bool Deleted { get; set; } = false;
        public string? CompanyWithdrawlComment { get; set; }
        public string? AgencyDeclineComment { get; set; }
        public string? UserEmailActioned { get; set; }
        public string? UserRoleActionedTo { get; set; }
        public string? UserEmailActionedTo { get; set; }
        public int ReviewCount { get; set; } = 0;
        public int AutoNew { get; set; } = 0;
        public int ManualNew { get; set; } = 0;
        public int ActiveView { get; set; } = 0;
        public int AllocateView { get; set; } = 0;
        public int InvestigateView { get; set; } = 0;
        public int VerifyView { get; set; } = 0;
        public int AssessView { get; set; } = 0;
        public int ManagerActiveView { get; set; } = 0;

        public DateTime? AllocatedToAgencyTime { get; set; }
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

        public bool UpdateAgentReport { get; set; } = false;
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