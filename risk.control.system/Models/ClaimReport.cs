using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ClaimReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ClaimReportId { get; set; } = Guid.NewGuid().ToString();

        public string? VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public DigitalIdReport? DigitalIdReport { get; set; } = new DigitalIdReport();

        public ReportQuestionaire ReportQuestionaire { get; set; } = new ReportQuestionaire();

        public DocumentIdReport? DocumentIdReport { get; set; } = new DocumentIdReport();

        public string? AgentEmail { get; set; }
        public DateTime? AgentRemarksUpdated { get; set; }
        public string? AgentRemarks { get; set; }
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

    public enum SupervisorRemarkType
    {
        OK,
        REVIEW
    }

    public enum AssessorRemarkType
    {
        OK,
        REVIEW,
    }

    public enum OcrImageType
    {
        PAN,
        ADHAAR,
        DL
    }
}