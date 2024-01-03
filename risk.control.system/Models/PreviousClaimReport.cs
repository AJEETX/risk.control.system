using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class PreviousClaimReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string PreviousClaimReportId { get; set; } = Guid.NewGuid().ToString();

        public string? VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public string? AgentEmail { get; set; }

        public DateTime? AgentRemarksUpdated { get; set; }
        public string? AgentRemarks { get; set; }
        public ReportQuestionaire ReportQuestionaire { get; set; } = new ReportQuestionaire();

        public DigitalIdReport? DigitalIdReport { get; set; } = new DigitalIdReport();

        public DocumentIdReport? DocumentIdReport { get; set; } = new DocumentIdReport();

        public DateTime? SupervisorRemarksUpdated { get; set; }
        public string? SupervisorEmail { get; set; }
        public string? SupervisorRemarks { get; set; }
        public SupervisorRemarkType? SupervisorRemarkType { get; set; }
        public DateTime? AssessorRemarksUpdated { get; set; }
        public string? AssessorEmail { get; set; }
        public string? AssessorRemarks { get; set; }
        public AssessorRemarkType? AssessorRemarkType { get; set; }

        public long CaseLocationId { get; set; }
        public CaseLocation CaseLocation { get; set; }
        public string? ServiceReportTemplateId { get; set; }
        public virtual ServiceReportTemplate? ServiceReportTemplate { get; set; }
    }
}