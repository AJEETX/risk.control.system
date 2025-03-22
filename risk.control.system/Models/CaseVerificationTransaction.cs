

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class CaseVerificationTransaction : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CaseVerificationTransactionId { get; set; }

        public long? CaseVerificationId { get; set; }
        public virtual CaseVerification? CaseVerification { get; set; }
        public string? InvestigationCaseStatusId { get; set; }
        public InvestigationCaseStatus? InvestigationCaseStatus { get; set; }
        public string? InvestigationCaseSubStatusId { get; set; }
        public InvestigationCaseSubStatus? InvestigationCaseSubStatus { get; set; }
        public int? Time2Update { get; set; } = int.MinValue;
        public int? HopCount { get; set; } = 0;
        public string TimeElapsed { get; set; } = "now";
        public string? CurrentClaimOwner { get; set; }
        public string? UserEmailActioned { get; set; }
        public string? UserRoleActionedTo { get; set; }
        public string? UserEmailActionedTo { get; set; }
        public bool IsReviewCase { get; set; } = false;
        public bool AgentAnswerEdited { get; set; } = false;
    }
}