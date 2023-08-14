using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class InvestigationTransaction : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string InvestigationTransactionId { get; set; } = Guid.NewGuid().ToString();

        public string? ClaimsInvestigationId { get; set; }
        public virtual ClaimsInvestigation? ClaimsInvestigation { get; set; }
        public string? InvestigationCaseStatusId { get; set; }
        public InvestigationCaseStatus? InvestigationCaseStatus { get; set; }
        public string? InvestigationCaseSubStatusId { get; set; }
        public InvestigationCaseSubStatus? InvestigationCaseSubStatus { get; set; }
        public int? Time2Update { get; set; } = int.MinValue;
        public int? HopCount { get; set; } = 0;
    }
}