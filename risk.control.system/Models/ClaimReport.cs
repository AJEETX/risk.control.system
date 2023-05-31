using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ClaimReport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ClaimReportId { get; set; } = Guid.NewGuid().ToString();

        public string? AgentRemarks { get; set; }
        public string? SupervisorRemarks { get; set; }
        public SupervisorRemarkType? SupervisorRemarkType { get; set; }
        public string? AssessorRemarks { get; set; }
        public AssessorRemarkType? AssessorRemarkType { get; set; }

        public long CaseLocationId { get; set; }
        public CaseLocation CaseLocation { get; set; }
    }

    public enum SupervisorRemarkType
    {
        OK,
        REVIEW
    }

    public enum AssessorRemarkType
    {
        OK,
        REVIEW,
        REJECT
    }
}